using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mentoragente.Application.Services;

public interface ISessionUpdateService
{
    Task UpdateSessionAfterMessageAsync(AgentSession session, AgentSessionData data, int durationDays);
    Task UpdateSessionForWelcomeMessageAsync(AgentSession session);
}

public class SessionUpdateService : ISessionUpdateService
{
    private readonly IAgentSessionRepository _agentSessionRepository;
    private readonly IAgentSessionDataRepository _agentSessionDataRepository;
    private readonly ILogger<SessionUpdateService> _logger;

    public SessionUpdateService(
        IAgentSessionRepository agentSessionRepository,
        IAgentSessionDataRepository agentSessionDataRepository,
        ILogger<SessionUpdateService> logger)
    {
        _agentSessionRepository = agentSessionRepository;
        _agentSessionDataRepository = agentSessionDataRepository;
        _logger = logger;
    }

    public async Task UpdateSessionAfterMessageAsync(AgentSession session, AgentSessionData data, int durationDays)
    {
        session.LastInteraction = DateTime.UtcNow;
        session.TotalMessages += 2;
        data.ProgressPercentage = CalculateProgress(session.TotalMessages, durationDays);

        await Task.WhenAll(
            _agentSessionRepository.UpdateAgentSessionAsync(session),
            _agentSessionDataRepository.UpdateAgentSessionDataAsync(data)
        );
    }

    public async Task UpdateSessionForWelcomeMessageAsync(AgentSession session)
    {
        session.LastInteraction = DateTime.UtcNow;
        session.TotalMessages = 2;
        await _agentSessionRepository.UpdateAgentSessionAsync(session);
    }

    private static int CalculateProgress(int totalMessages, int durationDays) =>
        Math.Min(100, (totalMessages * 100) / (durationDays * 10));
}

