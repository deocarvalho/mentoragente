using Mentoragente.Domain.Entities;

namespace Mentoragente.Domain.Interfaces;

public interface IAgentSessionRepository
{
    Task<AgentSession?> GetAgentSessionAsync(Guid userId, Guid mentoriaId);
    Task<AgentSession?> GetActiveAgentSessionAsync(Guid userId, Guid mentoriaId);
    Task<AgentSession?> GetAgentSessionByIdAsync(Guid id);
    Task<List<AgentSession>> GetAgentSessionsByUserIdAsync(Guid userId);
    Task<List<AgentSession>> GetAgentSessionsByUserIdAsync(Guid userId, int skip, int take);
    Task<int> GetAgentSessionsCountByUserIdAsync(Guid userId);
    Task<AgentSession> CreateAgentSessionAsync(AgentSession session);
    Task<AgentSession> UpdateAgentSessionAsync(AgentSession session);
}

