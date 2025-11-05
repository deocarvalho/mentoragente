using Mentoragente.Domain.Entities;

namespace Mentoragente.Domain.Interfaces;

public interface IAgentSessionDataRepository
{
    Task<AgentSessionData?> GetAgentSessionDataAsync(Guid agentSessionId);
    Task<AgentSessionData> CreateAgentSessionDataAsync(AgentSessionData data);
    Task<AgentSessionData> UpdateAgentSessionDataAsync(AgentSessionData data);
}

