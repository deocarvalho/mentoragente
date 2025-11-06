using Mentoragente.Domain.Entities;

namespace Mentoragente.Domain.Models;

public class AgentSessionWithData
{
    public AgentSession Session { get; set; } = null!;
    public AgentSessionData? Data { get; set; }
}

