using Mentoragente.Domain.Interfaces;
using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Enums;
using Microsoft.Extensions.Configuration;
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
    private readonly IMentorshipCacheService _mentorshipCacheService;
    private readonly IAgentSessionOrchestrationService _sessionOrchestrationService;
    private readonly IAccessValidationService _accessValidationService;
    private readonly IConversationRepository _conversationRepository;
    private readonly IOpenAIAssistantService _openAIAssistantService;
    private readonly IWhatsAppServiceFactory _whatsAppServiceFactory;
    private readonly ISessionUpdateService _sessionUpdateService;
    private readonly IPhoneNumberValidator _phoneNumberValidator;
    private readonly IInputSanitizer _inputSanitizer;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MessageProcessor> _logger;
    private const string DefaultUnauthorizedMessage = "Sinto muito, mas este número de telefone não tem acesso a este contato. Se você acredita que houve um engano, por favor, entre em contato com a pessoa gestora do seu contrato.";

    public MessageProcessor(
        IUserOrchestrationService userOrchestrationService,
        IMentorshipCacheService mentorshipCacheService,
        IAgentSessionOrchestrationService sessionOrchestrationService,
        IAccessValidationService accessValidationService,
        IConversationRepository conversationRepository,
        IOpenAIAssistantService openAIAssistantService,
        IWhatsAppServiceFactory whatsAppServiceFactory,
        ISessionUpdateService sessionUpdateService,
        IPhoneNumberValidator phoneNumberValidator,
        IInputSanitizer inputSanitizer,
        IConfiguration configuration,
        ILogger<MessageProcessor> logger)
    {
        _userOrchestrationService = userOrchestrationService;
        _mentorshipCacheService = mentorshipCacheService;
        _sessionOrchestrationService = sessionOrchestrationService;
        _accessValidationService = accessValidationService;
        _conversationRepository = conversationRepository;
        _openAIAssistantService = openAIAssistantService;
        _whatsAppServiceFactory = whatsAppServiceFactory;
        _sessionUpdateService = sessionUpdateService;
        _phoneNumberValidator = phoneNumberValidator;
        _inputSanitizer = inputSanitizer;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<MessageProcessingResult> ProcessMessageAsync(string phoneNumber, string messageText, Guid mentorshipId)
    {
        // Validação de número de telefone
        if (!_phoneNumberValidator.IsValidPhoneNumber(phoneNumber))
        {
            _logger.LogWarning("Invalid phone number format: {PhoneNumber}", phoneNumber);
            return await CreateUnauthorizedResponseAsync(mentorshipId);
        }

        // Sanitização de entrada
        messageText = _inputSanitizer.SanitizeMessage(messageText);

        if (string.IsNullOrWhiteSpace(messageText))
        {
            _logger.LogWarning("Received empty message from {PhoneNumber}", phoneNumber);
            return await CreateEmptyMessageResponseAsync(mentorshipId);
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
            return await CreateAccessExpiredResponseAsync(mentorshipId);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Session not found") || ex.Message.Contains("User not found"))
        {
            // Logging de tentativas não autorizadas (Segurança #3)
            _logger.LogWarning("Unauthorized access attempt - {Reason} - Phone: {PhoneNumber}, MentorshipId: {MentorshipId}", 
                ex.Message, phoneNumber, mentorshipId);
            
            return await CreateUnauthorizedResponseAsync(mentorshipId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message from {PhoneNumber}", phoneNumber);
            throw;
        }
    }

    private async Task<ProcessingContext> LoadProcessingContextAsync(string phoneNumber, Guid mentorshipId)
    {
        // Buscar usuário (NÃO criar) - usuários só são criados pelo enrollment
        var user = await _userOrchestrationService.GetUserAsync(phoneNumber);
        if (user == null)
        {
            _logger.LogWarning("User not found for phone {PhoneNumber}", phoneNumber);
            throw new InvalidOperationException("User not found");
        }

        // Validar mentorship existe e está ativo (Segurança #4)
        var mentorship = await GetMentorshipOrThrowAsync(mentorshipId);
        if (mentorship.Status != MentorshipStatus.Active)
        {
            _logger.LogWarning("Mentorship {MentorshipId} is not active (Status: {Status})", mentorshipId, mentorship.Status);
            throw new InvalidOperationException($"Mentorship {mentorshipId} is not active");
        }
        
        // Buscar sessão (NÃO criar) - sessões só são criadas pelo enrollment
        var sessionContext = await _sessionOrchestrationService.GetSessionContextAsync(
            user.Id, mentorshipId);

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
        var mentorship = await _mentorshipCacheService.GetMentorshipAsync(mentorshipId);
        if (mentorship == null)
        {
            _logger.LogError("Mentorship {MentorshipId} not found", mentorshipId);
            throw new InvalidOperationException($"Mentorship {mentorshipId} not found");
        }
        return mentorship;
    }

    private async Task<string> ProcessWithAIAsync(ProcessingContext context, string messageText, string assistantId)
    {
        // Sanitizar mensagem antes de enviar para IA
        var sanitizedMessage = _inputSanitizer.SanitizeMessage(messageText);
        await _openAIAssistantService.AddUserMessageAsync(context.Session.AIContextId!, sanitizedMessage);
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

            var startPrompt = $"Olá! Me chamo {displayName} e estou começando agora em {context.Mentorship.Name}! " + 
                "Me dá as boas vindas e explique resumidamente o que vamos fazer aqui.";

            var welcomeMessage = await ProcessWithAIAsync(context, startPrompt, context.Mentorship.AssistantId);

            await SaveConversationAsync(context.Session.Id, startPrompt, welcomeMessage);

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

    private async Task<MessageProcessingResult> CreateUnauthorizedResponseAsync(Guid mentorshipId) =>
    await CreateMentorshipExceptionResponseAsync(mentorshipId, _configuration["Messages:UnauthorizedAccess"] ?? DefaultUnauthorizedMessage);

    private async Task<MessageProcessingResult> CreateAccessExpiredResponseAsync(Guid mentorshipId) => 
    await CreateMentorshipExceptionResponseAsync(mentorshipId, "Your access period to this mentorship has ended. Please contact to renew.");

    private async Task<MessageProcessingResult> CreateEmptyMessageResponseAsync(Guid mentorshipId) =>
    await CreateMentorshipExceptionResponseAsync(mentorshipId, "Sorry, I couldn't understand your message. Please send a message with text.");

    private async Task<MessageProcessingResult> CreateMentorshipExceptionResponseAsync(Guid mentorshipId, string message)
    {
        var mentorship = await GetMentorshipOrThrowAsync(mentorshipId);
        return new MessageProcessingResult
        {
            Response = message,
            Mentorship = mentorship
        };
    }
}

