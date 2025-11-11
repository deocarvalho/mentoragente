using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Enums;
using Mentoragente.Domain.Interfaces;
using Mentoragente.Domain.Models;
using Microsoft.Extensions.Logging;

namespace Mentoragente.Application.Services;

public interface IAgentSessionOrchestrationService
{
    Task<AgentSessionContext> GetOrCreateSessionContextAsync(Guid userId, Guid mentorshipId, int durationDays);
    Task EnsureThreadExistsAsync(AgentSession session);
}

public class AgentSessionContext
{
    public AgentSession Session { get; set; } = null!;
    public AgentSessionData Data { get; set; } = null!;
}

public class AgentSessionOrchestrationService : IAgentSessionOrchestrationService
{
    private readonly IAgentSessionRepository _agentSessionRepository;
    private readonly IAgentSessionDataRepository _agentSessionDataRepository;
    private readonly IOpenAIAssistantService _openAIAssistantService;
    private readonly ILogger<AgentSessionOrchestrationService> _logger;

    public AgentSessionOrchestrationService(
        IAgentSessionRepository agentSessionRepository,
        IAgentSessionDataRepository agentSessionDataRepository,
        IOpenAIAssistantService openAIAssistantService,
        ILogger<AgentSessionOrchestrationService> logger)
    {
        _agentSessionRepository = agentSessionRepository;
        _agentSessionDataRepository = agentSessionDataRepository;
        _openAIAssistantService = openAIAssistantService;
        _logger = logger;
    }

    public async Task<AgentSessionContext> GetOrCreateSessionContextAsync(Guid userId, Guid mentorshipId, int durationDays)
    {
        var existingContext = await GetExistingSessionContextAsync(userId, mentorshipId);
        if (existingContext != null)
            return existingContext;

        return await CreateNewSessionContextAsync(userId, mentorshipId, durationDays);
    }

    private async Task<AgentSessionContext?> GetExistingSessionContextAsync(Guid userId, Guid mentorshipId)
    {
        var sessionWithData = await _agentSessionRepository.GetActiveAgentSessionWithDataAsync(userId, mentorshipId);
        if (sessionWithData?.Data != null)
            return new AgentSessionContext { Session = sessionWithData.Session, Data = sessionWithData.Data };

        var anySessionWithData = await _agentSessionRepository.GetAgentSessionWithDataAsync(userId, mentorshipId);
        if (anySessionWithData?.Data == null)
            return null;

        return await HandleExistingInactiveSessionAsync(anySessionWithData.Session, anySessionWithData.Data);
    }

    private async Task<AgentSessionContext?> HandleExistingInactiveSessionAsync(AgentSession session, AgentSessionData data)
    {
        if (DateTime.UtcNow > data.AccessEndDate)
        {
            session.Status = AgentSessionStatus.Expired;
            await _agentSessionRepository.UpdateAgentSessionAsync(session);
            _logger.LogWarning("Access expired for session {SessionId}", session.Id);
            throw new InvalidOperationException("Access expired");
        }

        session.Status = AgentSessionStatus.Active;
        session = await _agentSessionRepository.UpdateAgentSessionAsync(session);
        return new AgentSessionContext { Session = session, Data = data };
    }

    private async Task<AgentSessionContext> CreateNewSessionContextAsync(Guid userId, Guid mentorshipId, int durationDays)
    {
        var session = new AgentSession
        {
            UserId = userId,
            MentorshipId = mentorshipId,
            Status = AgentSessionStatus.Active
        };
        session = await _agentSessionRepository.CreateAgentSessionAsync(session);

        var data = new AgentSessionData
        {
            AgentSessionId = session.Id,
            AccessStartDate = DateTime.UtcNow,
            AccessEndDate = DateTime.UtcNow.AddDays(durationDays),
            ProgressPercentage = 0
        };
        data = await _agentSessionDataRepository.CreateAgentSessionDataAsync(data);

        _logger.LogInformation("Created new session {SessionId} for user {UserId} and mentorship {MentorshipId}", 
            session.Id, userId, mentorshipId);

        return new AgentSessionContext { Session = session, Data = data };
    }

    public async Task EnsureThreadExistsAsync(AgentSession session)
    {
        if (!string.IsNullOrEmpty(session.AIContextId))
            return;

        var threadId = await _openAIAssistantService.CreateThreadAsync();
        session.AIContextId = threadId;
        await _agentSessionRepository.UpdateAgentSessionAsync(session);
        _logger.LogInformation("Created OpenAI thread {ThreadId} for agent session {AgentSessionId}", threadId, session.Id);
    }
}

