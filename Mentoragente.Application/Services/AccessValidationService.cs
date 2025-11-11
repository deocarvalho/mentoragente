using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Enums;
using Mentoragente.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mentoragente.Application.Services;

public interface IAccessValidationService
{
    Task<AccessValidationResult> ValidateAccessAsync(AgentSession session, AgentSessionData data);
}

public class AccessValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
}

public class AccessValidationService : IAccessValidationService
{
    private readonly IAgentSessionRepository _agentSessionRepository;
    private readonly ILogger<AccessValidationService> _logger;

    public AccessValidationService(
        IAgentSessionRepository agentSessionRepository,
        ILogger<AccessValidationService> logger)
    {
        _agentSessionRepository = agentSessionRepository;
        _logger = logger;
    }

    public async Task<AccessValidationResult> ValidateAccessAsync(AgentSession session, AgentSessionData data)
    {
        if (IsAccessExpired(data))
        {
            await MarkSessionAsExpiredAsync(session);
            return CreateExpiredResult();
        }

        return new AccessValidationResult { IsValid = true };
    }

    private static bool IsAccessExpired(AgentSessionData data) => DateTime.UtcNow > data.AccessEndDate;

    private async Task MarkSessionAsExpiredAsync(AgentSession session)
    {
        session.Status = AgentSessionStatus.Expired;
        await _agentSessionRepository.UpdateAgentSessionAsync(session);
        _logger.LogWarning("Access expired for session {SessionId}", session.Id);
    }

    private static AccessValidationResult CreateExpiredResult() => new()
    {
        IsValid = false,
        ErrorMessage = "Your access period to this mentorship has ended. Please contact to renew."
    };
}

