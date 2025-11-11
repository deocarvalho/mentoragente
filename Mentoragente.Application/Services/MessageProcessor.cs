using Mentoragente.Domain.Interfaces;
using Mentoragente.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Mentoragente.Application.Services;

public interface IMessageProcessor
{
    Task<MessageProcessingResult> ProcessMessageAsync(string phoneNumber, string messageText, Guid mentorshipId);
    Task<bool> SendWelcomeMessageAsync(string phoneNumber, Guid mentorshipId, string? userName = null);
}

public class MessageProcessingResult
{
    public string Response { get; set; } = null!;
    public Mentorship Mentorship { get; set; } = null!;
}

public class MessageProcessor : IMessageProcessor
{
    private readonly IUserOrchestrationService _userOrchestrationService;
    private readonly IMentorshipRepository _mentorshipRepository;
    private readonly IAgentSessionOrchestrationService _sessionOrchestrationService;
    private readonly IAccessValidationService _accessValidationService;
    private readonly IConversationRepository _conversationRepository;
    private readonly IOpenAIAssistantService _openAIAssistantService;
    private readonly IWhatsAppServiceFactory _whatsAppServiceFactory;
    private readonly ISessionUpdateService _sessionUpdateService;
    private readonly ILogger<MessageProcessor> _logger;

    public MessageProcessor(
        IUserOrchestrationService userOrchestrationService,
        IMentorshipRepository mentorshipRepository,
        IAgentSessionOrchestrationService sessionOrchestrationService,
        IAccessValidationService accessValidationService,
        IConversationRepository conversationRepository,
        IOpenAIAssistantService openAIAssistantService,
        IWhatsAppServiceFactory whatsAppServiceFactory,
        ISessionUpdateService sessionUpdateService,
        ILogger<MessageProcessor> logger)
    {
        _userOrchestrationService = userOrchestrationService;
        _mentorshipRepository = mentorshipRepository;
        _sessionOrchestrationService = sessionOrchestrationService;
        _accessValidationService = accessValidationService;
        _conversationRepository = conversationRepository;
        _openAIAssistantService = openAIAssistantService;
        _whatsAppServiceFactory = whatsAppServiceFactory;
        _sessionUpdateService = sessionUpdateService;
        _logger = logger;
    }

    public async Task<MessageProcessingResult> ProcessMessageAsync(string phoneNumber, string messageText, Guid mentorshipId)
    {
        if (string.IsNullOrWhiteSpace(messageText))
        {
            _logger.LogWarning("Received empty message from {PhoneNumber}", phoneNumber);
            var mentorship = await GetMentorshipOrThrowAsync(mentorshipId);
            return new MessageProcessingResult
            {
                Response = "Sorry, I couldn't understand your message. Please send a message with text.",
                Mentorship = mentorship
            };
        }

        _logger.LogInformation("Processing message from {PhoneNumber} for mentorship {MentorshipId}: {Message}", 
            phoneNumber, mentorshipId, messageText);

        try
        {
            var context = await LoadProcessingContextAsync(phoneNumber, mentorshipId);
            var validationResult = await _accessValidationService.ValidateAccessAsync(context.Session, context.Data);
            
            if (!validationResult.IsValid)
            {
                return new MessageProcessingResult
                {
                    Response = validationResult.ErrorMessage!,
                    Mentorship = context.Mentorship
                };
            }

            await _sessionOrchestrationService.EnsureThreadExistsAsync(context.Session);
            
            var responseText = await ProcessWithAIAsync(context, messageText, context.Mentorship.AssistantId);
            
            await SaveConversationAsync(context.Session.Id, messageText, responseText);
            await _sessionUpdateService.UpdateSessionAfterMessageAsync(context.Session, context.Data, context.Mentorship.DurationDays);

            _logger.LogInformation("Successfully processed message from {PhoneNumber}", phoneNumber);
            return new MessageProcessingResult
            {
                Response = responseText,
                Mentorship = context.Mentorship
            };
        }
        catch (InvalidOperationException ex) when (ex.Message == "Access expired")
        {
            var mentorship = await GetMentorshipOrThrowAsync(mentorshipId);
            return new MessageProcessingResult
            {
                Response = "Your access period to this mentorship has ended. Please contact to renew.",
                Mentorship = mentorship
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message from {PhoneNumber}", phoneNumber);
            throw;
        }
    }

    private async Task<ProcessingContext> LoadProcessingContextAsync(string phoneNumber, Guid mentorshipId)
    {
        var user = await _userOrchestrationService.GetOrCreateUserAsync(phoneNumber);
        var mentorship = await GetMentorshipOrThrowAsync(mentorshipId);
        var sessionContext = await _sessionOrchestrationService.GetOrCreateSessionContextAsync(
            user.Id, mentorshipId, mentorship.DurationDays);

        return new ProcessingContext
        {
            User = user,
            Mentorship = mentorship,
            Session = sessionContext.Session,
            Data = sessionContext.Data
        };
    }

    private async Task<Mentorship> GetMentorshipOrThrowAsync(Guid mentorshipId)
    {
        var mentorship = await _mentorshipRepository.GetMentorshipByIdAsync(mentorshipId);
        if (mentorship == null)
        {
            _logger.LogError("Mentorship {MentorshipId} not found", mentorshipId);
            throw new InvalidOperationException($"Mentorship {mentorshipId} not found");
        }
        return mentorship;
    }

    private async Task<string> ProcessWithAIAsync(ProcessingContext context, string messageText, string assistantId)
    {
        await _openAIAssistantService.AddUserMessageAsync(context.Session.AIContextId!, messageText);
        return await _openAIAssistantService.RunAssistantAsync(context.Session.AIContextId!, assistantId);
    }

    private async Task SaveConversationAsync(Guid sessionId, string userMessage, string assistantMessage)
    {
        await Task.WhenAll(
            _conversationRepository.AddMessageAsync(sessionId, "user", userMessage),
            _conversationRepository.AddMessageAsync(sessionId, "assistant", assistantMessage)
        );
    }

    private class ProcessingContext
    {
        public User User { get; set; } = null!;
        public Mentorship Mentorship { get; set; } = null!;
        public AgentSession Session { get; set; } = null!;
        public AgentSessionData Data { get; set; } = null!;
    }

    public async Task<bool> SendWelcomeMessageAsync(string phoneNumber, Guid mentorshipId, string? userName = null)
    {
        _logger.LogInformation("Sending welcome message to {PhoneNumber} for mentorship {MentorshipId}", phoneNumber, mentorshipId);

        try
        {
            var context = await LoadProcessingContextAsync(phoneNumber, mentorshipId);
            var displayName = userName ?? context.User.Name ?? "there";

            if (await IsWelcomeMessageAlreadySentAsync(context.Session.Id))
            {
                _logger.LogInformation("Welcome message already sent for session {SessionId}", context.Session.Id);
                return true;
            }

            await _sessionOrchestrationService.EnsureThreadExistsAsync(context.Session);

            var welcomePrompt = BuildWelcomePrompt(displayName, context.Mentorship);
            var welcomeMessage = await ProcessWithAIAsync(context, welcomePrompt, context.Mentorship.AssistantId);

            await SaveConversationAsync(context.Session.Id, welcomePrompt, welcomeMessage);

            // Get the correct service based on mentorship configuration
            var whatsAppService = _whatsAppServiceFactory.GetServiceForMentorship(context.Mentorship);
            var sent = await whatsAppService.SendMessageAsync(phoneNumber, welcomeMessage, context.Mentorship);

            if (sent)
            {
                await _sessionUpdateService.UpdateSessionForWelcomeMessageAsync(context.Session);
                _logger.LogInformation("Welcome message sent successfully to {PhoneNumber}", phoneNumber);
            }
            else
            {
                _logger.LogWarning("Failed to send welcome message to {PhoneNumber} via {Provider}", 
                    phoneNumber, context.Mentorship.WhatsAppProvider);
            }

            return sent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending welcome message to {PhoneNumber}", phoneNumber);
            return false;
        }
    }

    private async Task<bool> IsWelcomeMessageAlreadySentAsync(Guid sessionId)
    {
        var existingMessages = await _conversationRepository.GetConversationHistoryAsync(sessionId);
        return existingMessages.Any(m => m.Role == "assistant");
    }

    private static string BuildWelcomePrompt(string userName, Mentorship mentorship) =>
        $"Welcome {userName} to the {mentorship.Name} program! " +
        $"This is a {mentorship.DurationDays}-day mentorship program. " +
        $"Introduce yourself as their AI mentor assistant and explain what they can expect during this journey. " +
        $"Be warm, friendly, and encouraging. Start the conversation naturally and make them feel welcomed.";
}

