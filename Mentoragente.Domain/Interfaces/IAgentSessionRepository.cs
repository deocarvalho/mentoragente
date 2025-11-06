using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Models;

namespace Mentoragente.Domain.Interfaces;

public interface IAgentSessionRepository
{
    Task<AgentSession?> GetAgentSessionAsync(Guid userId, Guid mentorshipId);
    Task<AgentSession?> GetActiveAgentSessionAsync(Guid userId, Guid mentorshipId);
    Task<AgentSession?> GetAgentSessionByIdAsync(Guid id);
    Task<AgentSessionWithData?> GetActiveAgentSessionWithDataAsync(Guid userId, Guid mentorshipId);
    Task<AgentSessionWithData?> GetAgentSessionWithDataAsync(Guid userId, Guid mentorshipId);
    Task<List<AgentSession>> GetAgentSessionsByUserIdAsync(Guid userId);
    Task<List<AgentSession>> GetAgentSessionsByUserIdAsync(Guid userId, int skip, int take);
    Task<int> GetAgentSessionsCountByUserIdAsync(Guid userId);
    Task<AgentSession> CreateAgentSessionAsync(AgentSession session);
    Task<AgentSession> UpdateAgentSessionAsync(AgentSession session);
}

