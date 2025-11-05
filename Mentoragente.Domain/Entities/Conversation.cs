using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Mentoragente.Domain.Entities;

[Table("conversations")]
public class Conversation : BaseModel
{
    [PrimaryKey("id")]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Column("agent_session_id")]
    public Guid AgentSessionId { get; set; }
    
    [Column("sender")]
    public string Sender { get; set; } = string.Empty;
    
    [Column("message")]
    public string Message { get; set; } = string.Empty;
    
    [Column("message_type")]
    public string MessageType { get; set; } = "text";
    
    [Column("tokens_used")]
    public int? TokensUsed { get; set; }
    
    [Column("response_time_ms")]
    public int? ResponseTimeMs { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

