using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Models;

namespace Mentoragente.Domain.Interfaces;

public interface IConversationRepository
{
    Task<List<ChatMessage>> GetConversationHistoryAsync(Guid agentSessionId);
    Task AddMessageAsync(Guid agentSessionId, string role, string content);
    Task ClearConversationAsync(Guid agentSessionId);
    Task<string> GetConversationThreadIdAsync(Guid agentSessionId);
}

