using Mentoragente.Domain.Interfaces;
using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Mentoragente.Application.Services;

public interface IMessageProcessor
{
    Task<string> ProcessMessageAsync(string phoneNumber, string messageText, Guid mentorshipId);
}

public class MessageProcessor : IMessageProcessor
{
    private readonly IUserRepository _userRepository;
    private readonly IMentorshipRepository _mentorshipRepository;
    private readonly IAgentSessionRepository _agentSessionRepository;
    private readonly IAgentSessionDataRepository _agentSessionDataRepository;
    private readonly IConversationRepository _conversationRepository;
    private readonly IOpenAIAssistantService _openAIAssistantService;
    private readonly ILogger<MessageProcessor> _logger;

    public MessageProcessor(
        IUserRepository userRepository,
        IMentorshipRepository mentorshipRepository,
        IAgentSessionRepository agentSessionRepository,
        IAgentSessionDataRepository agentSessionDataRepository,
        IConversationRepository conversationRepository,
        IOpenAIAssistantService openAIAssistantService,
        ILogger<MessageProcessor> logger)
    {
        _userRepository = userRepository;
        _mentorshipRepository = mentorshipRepository;
        _agentSessionRepository = agentSessionRepository;
        _agentSessionDataRepository = agentSessionDataRepository;
        _conversationRepository = conversationRepository;
        _openAIAssistantService = openAIAssistantService;
        _logger = logger;
    }

    public async Task<string> ProcessMessageAsync(string phoneNumber, string messageText, Guid mentorshipId)
    {
        try
        {
            // Validate empty message
            if (string.IsNullOrWhiteSpace(messageText))
            {
                _logger.LogWarning("Received empty message from {PhoneNumber}", phoneNumber);
                return "Sorry, I couldn't understand your message. Please send a message with text.";
            }

            _logger.LogInformation("Processing message from {PhoneNumber} for mentorship {MentorshipId}: {Message}", phoneNumber, mentorshipId, messageText);

            // 1. Find or create User
            var user = await _userRepository.GetUserByPhoneAsync(phoneNumber);
            if (user == null)
            {
                user = new User
                {
                    PhoneNumber = phoneNumber,
                    Name = "WhatsApp Client",
                    Status = UserStatus.Active
                };
                user = await _userRepository.CreateUserAsync(user);
                _logger.LogInformation("Created new user {UserId} for phone {PhoneNumber}", user.Id, phoneNumber);
            }

            // 2. Find Mentorship
            var mentorship = await _mentorshipRepository.GetMentorshipByIdAsync(mentorshipId);
            if (mentorship == null)
            {
                _logger.LogError("Mentorship {MentorshipId} not found", mentorshipId);
                throw new InvalidOperationException($"Mentorship {mentorshipId} not found");
            }

            // 3. Find or create AgentSession with data (optimized: fetch both in one go)
            var sessionWithData = await _agentSessionRepository.GetActiveAgentSessionWithDataAsync(user.Id, mentorshipId);
            AgentSession? agentSession = null;
            AgentSessionData? accessData = null;

            if (sessionWithData != null)
            {
                agentSession = sessionWithData.Session;
                accessData = sessionWithData.Data;
            }
            else
            {
                // Check if any session exists (even if not active)
                var existingSessionWithData = await _agentSessionRepository.GetAgentSessionWithDataAsync(user.Id, mentorshipId);
                
                if (existingSessionWithData != null)
                {
                    agentSession = existingSessionWithData.Session;
                    accessData = existingSessionWithData.Data;
                    
                    if (accessData != null && DateTime.UtcNow > accessData.AccessEndDate)
                    {
                        // Access expired
                        agentSession.Status = AgentSessionStatus.Expired;
                        await _agentSessionRepository.UpdateAgentSessionAsync(agentSession);
                        _logger.LogWarning("Access expired for user {UserId} in mentorship {MentorshipId}", user.Id, mentorshipId);
                        return "Your access period to this mentorship has ended. Please contact to renew.";
                    }
                    else if (accessData != null && DateTime.UtcNow <= accessData.AccessEndDate)
                    {
                        // Reactivate session
                        agentSession.Status = AgentSessionStatus.Active;
                        agentSession = await _agentSessionRepository.UpdateAgentSessionAsync(agentSession);
                    }
                }
            }

            // Create new session if not found
            if (agentSession == null)
            {
                agentSession = new AgentSession
                {
                    UserId = user.Id,
                    MentorshipId = mentorshipId,
                    Status = AgentSessionStatus.Active
                };
                agentSession = await _agentSessionRepository.CreateAgentSessionAsync(agentSession);

                // Create AgentSessionData
                accessData = new AgentSessionData
                {
                    AgentSessionId = agentSession.Id,
                    AccessStartDate = DateTime.UtcNow,
                    AccessEndDate = DateTime.UtcNow.AddDays(mentorship.DurationDays),
                    ProgressPercentage = 0
                };
                accessData = await _agentSessionDataRepository.CreateAgentSessionDataAsync(accessData);
                _logger.LogInformation("Created new agent session {AgentSessionId} for user {UserId} and mentorship {MentorshipId}", agentSession.Id, user.Id, mentorshipId);
            }

            // 4. Validate access (accessData already loaded above)
            if (accessData == null)
            {
                _logger.LogError("AgentSessionData not found for session {AgentSessionId}", agentSession.Id);
                throw new InvalidOperationException("Session data not found");
            }

            if (DateTime.UtcNow > accessData.AccessEndDate)
            {
                agentSession.Status = AgentSessionStatus.Expired;
                await _agentSessionRepository.UpdateAgentSessionAsync(agentSession);
                return "Your access period to this mentorship has ended. Please contact to renew.";
            }

            // 5. Create Thread ID if it doesn't exist
            if (string.IsNullOrEmpty(agentSession.AIContextId))
            {
                var threadId = await _openAIAssistantService.CreateThreadAsync();
                agentSession.AIContextId = threadId;
                // Will be updated together with other session updates at the end
                _logger.LogInformation("Created OpenAI thread {ThreadId} for agent session {AgentSessionId}", threadId, agentSession.Id);
            }

            // 6. Add user message to local history
            await _conversationRepository.AddMessageAsync(agentSession.Id, "user", messageText);

            // 7. Send message to OpenAI Assistant
            if (string.IsNullOrEmpty(agentSession.AIContextId))
            {
                throw new InvalidOperationException($"AgentSession {agentSession.Id} has no AIContextId after validation");
            }
            await _openAIAssistantService.AddUserMessageAsync(agentSession.AIContextId, messageText);

            // 8. Get assistant response
            var responseText = await _openAIAssistantService.RunAssistantAsync(agentSession.AIContextId, mentorship.AssistantId);

            // 9. Add response to local history
            await _conversationRepository.AddMessageAsync(agentSession.Id, "assistant", responseText);

            // 10. Batch update session and progress (optimized: update both in parallel)
            agentSession.LastInteraction = DateTime.UtcNow;
            agentSession.TotalMessages += 2; // user + assistant
            accessData.ProgressPercentage = Math.Min(100, (agentSession.TotalMessages * 100) / (mentorship.DurationDays * 10)); // Estimate

            // Update both in parallel to reduce total time
            var updateSessionTask = _agentSessionRepository.UpdateAgentSessionAsync(agentSession);
            var updateDataTask = _agentSessionDataRepository.UpdateAgentSessionDataAsync(accessData);
            await Task.WhenAll(updateSessionTask, updateDataTask);

            _logger.LogInformation("Successfully processed message from {PhoneNumber} for mentorship {MentorshipId}", phoneNumber, mentorshipId);

            return responseText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message from {PhoneNumber}", phoneNumber);
            throw;
        }
    }
}

