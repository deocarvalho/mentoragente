using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using Mentoragente.Domain.Enums;

namespace Mentoragente.Domain.Entities;

[Table("agent_sessions")]
public class AgentSession : BaseModel
{
    [PrimaryKey("id")]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Column("user_id")]
    public Guid UserId { get; set; }
    
    [Column("mentoria_id")]
    public Guid MentoriaId { get; set; }
    
    [Column("ai_provider")]
    public AIProvider AIProvider { get; set; } = AIProvider.OpenAI;
    
    [Column("ai_context_id")]
    public string? AIContextId { get; set; }
    
    [Column("status")]
    public AgentSessionStatus Status { get; set; } = AgentSessionStatus.Active;
    
    [Column("last_interaction")]
    public DateTime? LastInteraction { get; set; }
    
    [Column("total_messages")]
    public int TotalMessages { get; set; } = 0;
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

