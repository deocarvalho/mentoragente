using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Enums;
using Mentoragente.Domain.Interfaces;
using Mentoragente.Application.Models;
using Microsoft.Extensions.Logging;

namespace Mentoragente.Application.Services;

public interface IAgentSessionService
{
    Task<AgentSession?> GetAgentSessionByIdAsync(Guid id);
    Task<AgentSession?> GetAgentSessionAsync(Guid userId, Guid mentorshipId);
    Task<List<AgentSession>> GetAgentSessionsByUserIdAsync(Guid userId);
    Task<PagedResult<AgentSession>> GetAgentSessionsByUserIdAsync(Guid userId, int page = 1, int pageSize = 10);
    Task<AgentSession?> GetActiveAgentSessionAsync(Guid userId, Guid mentorshipId);
    Task<AgentSession> CreateAgentSessionAsync(Guid userId, Guid mentorshipId, string? aiContextId = null);
    Task<AgentSession> UpdateAgentSessionAsync(
        Guid id,
        AgentSessionStatus? status = null,
        string? aiContextId = null,
        DateTime? lastInteraction = null);
    Task<bool> ExpireSessionAsync(Guid id);
    Task<bool> PauseSessionAsync(Guid id);
    Task<bool> ResumeSessionAsync(Guid id);
}

public class AgentSessionService : IAgentSessionService
{
    private readonly IAgentSessionRepository _agentSessionRepository;
    private readonly IAgentSessionDataRepository _agentSessionDataRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMentorshipRepository _mentorshipRepository;
    private readonly ILogger<AgentSessionService> _logger;

    public AgentSessionService(
        IAgentSessionRepository agentSessionRepository,
        IAgentSessionDataRepository agentSessionDataRepository,
        IUserRepository userRepository,
        IMentorshipRepository mentorshipRepository,
        ILogger<AgentSessionService> logger)
    {
        _agentSessionRepository = agentSessionRepository;
        _agentSessionDataRepository = agentSessionDataRepository;
        _userRepository = userRepository;
        _mentorshipRepository = mentorshipRepository;
        _logger = logger;
    }

    public async Task<AgentSession?> GetAgentSessionByIdAsync(Guid id)
    {
        _logger.LogInformation("Getting agent session by ID: {SessionId}", id);
        return await _agentSessionRepository.GetAgentSessionByIdAsync(id);
    }

    public async Task<AgentSession?> GetAgentSessionAsync(Guid userId, Guid mentorshipId)
    {
        _logger.LogInformation("Getting agent session for user {UserId} and mentorship {MentorshipId}", userId, mentorshipId);
        return await _agentSessionRepository.GetAgentSessionAsync(userId, mentorshipId);
    }

    public async Task<List<AgentSession>> GetAgentSessionsByUserIdAsync(Guid userId)
    {
        _logger.LogInformation("Getting all agent sessions for user {UserId}", userId);
        return await _agentSessionRepository.GetAgentSessionsByUserIdAsync(userId);
    }

    public async Task<PagedResult<AgentSession>> GetAgentSessionsByUserIdAsync(Guid userId, int page = 1, int pageSize = 10)
    {
        _logger.LogInformation("Getting agent sessions for user {UserId} - Page: {Page}, PageSize: {PageSize}", userId, page, pageSize);
        var skip = (page - 1) * pageSize;
        var sessions = await _agentSessionRepository.GetAgentSessionsByUserIdAsync(userId, skip, pageSize);
        var total = await _agentSessionRepository.GetAgentSessionsCountByUserIdAsync(userId);
        return PagedResult<AgentSession>.Create(sessions, total, page, pageSize);
    }

    public async Task<AgentSession?> GetActiveAgentSessionAsync(Guid userId, Guid mentorshipId)
    {
        _logger.LogInformation("Getting active agent session for user {UserId} and mentorship {MentorshipId}", userId, mentorshipId);
        return await _agentSessionRepository.GetActiveAgentSessionAsync(userId, mentorshipId);
    }

    public async Task<AgentSession> CreateAgentSessionAsync(Guid userId, Guid mentorshipId, string? aiContextId = null)
    {
        // Validate user exists
        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user == null)
        {
            _logger.LogError("User {UserId} not found", userId);
            throw new InvalidOperationException($"User with ID {userId} not found");
        }

        // Validate mentorship exists
        var mentorship = await _mentorshipRepository.GetMentorshipByIdAsync(mentorshipId);
        if (mentorship == null)
        {
            _logger.LogError("Mentorship {MentorshipId} not found", mentorshipId);
            throw new InvalidOperationException($"Mentorship with ID {mentorshipId} not found");
        }

        // Check if active session already exists
        var existingSession = await _agentSessionRepository.GetActiveAgentSessionAsync(userId, mentorshipId);
        if (existingSession != null)
        {
            _logger.LogWarning("Active session already exists for user {UserId} and mentorship {MentorshipId}: {SessionId}", 
                userId, mentorshipId, existingSession.Id);
            throw new InvalidOperationException("Active session already exists for this user and mentorship");
        }

        var session = new AgentSession
        {
            UserId = userId,
            MentorshipId = mentorshipId,
            AIProvider = AIProvider.OpenAI,
            AIContextId = aiContextId,
            Status = AgentSessionStatus.Active,
            LastInteraction = DateTime.UtcNow,
            TotalMessages = 0
        };

        _logger.LogInformation("Creating new agent session for user {UserId} and mentorship {MentorshipId}", userId, mentorshipId);
        var createdSession = await _agentSessionRepository.CreateAgentSessionAsync(session);

        // Create AgentSessionData
        var sessionData = new AgentSessionData
        {
            AgentSessionId = createdSession.Id,
            AccessStartDate = DateTime.UtcNow,
            AccessEndDate = DateTime.UtcNow.AddDays(mentorship.DurationDays),
            ProgressPercentage = 0,
            ReportGenerated = false
        };

        await _agentSessionDataRepository.CreateAgentSessionDataAsync(sessionData);
        _logger.LogInformation("Created agent session data for session {SessionId}", createdSession.Id);

        return createdSession;
    }

    public async Task<AgentSession> UpdateAgentSessionAsync(
        Guid id,
        AgentSessionStatus? status = null,
        string? aiContextId = null,
        DateTime? lastInteraction = null)
    {
        var session = await _agentSessionRepository.GetAgentSessionByIdAsync(id);
        if (session == null)
        {
            _logger.LogError("Agent session {SessionId} not found for update", id);
            throw new InvalidOperationException($"Agent session with ID {id} not found");
        }

        if (status.HasValue)
            session.Status = status.Value;

        if (aiContextId != null)
            session.AIContextId = aiContextId;

        if (lastInteraction.HasValue)
            session.LastInteraction = lastInteraction.Value;

        _logger.LogInformation("Updating agent session {SessionId}", id);
        return await _agentSessionRepository.UpdateAgentSessionAsync(session);
    }

    public async Task<bool> ExpireSessionAsync(Guid id)
    {
        var session = await _agentSessionRepository.GetAgentSessionByIdAsync(id);
        if (session == null)
        {
            _logger.LogWarning("Agent session {SessionId} not found for expiration", id);
            return false;
        }

        session.Status = AgentSessionStatus.Expired;
        await _agentSessionRepository.UpdateAgentSessionAsync(session);
        
        _logger.LogInformation("Expired agent session {SessionId}", id);
        return true;
    }

    public async Task<bool> PauseSessionAsync(Guid id)
    {
        var session = await _agentSessionRepository.GetAgentSessionByIdAsync(id);
        if (session == null)
        {
            _logger.LogWarning("Agent session {SessionId} not found for pause", id);
            return false;
        }

        session.Status = AgentSessionStatus.Paused;
        await _agentSessionRepository.UpdateAgentSessionAsync(session);
        
        _logger.LogInformation("Paused agent session {SessionId}", id);
        return true;
    }

    public async Task<bool> ResumeSessionAsync(Guid id)
    {
        var session = await _agentSessionRepository.GetAgentSessionByIdAsync(id);
        if (session == null)
        {
            _logger.LogWarning("Agent session {SessionId} not found for resume", id);
            return false;
        }

        session.Status = AgentSessionStatus.Active;
        await _agentSessionRepository.UpdateAgentSessionAsync(session);
        
        _logger.LogInformation("Resumed agent session {SessionId}", id);
        return true;
    }
}

