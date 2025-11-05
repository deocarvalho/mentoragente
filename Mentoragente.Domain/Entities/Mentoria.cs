using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using Mentoragente.Domain.Enums;

namespace Mentoragente.Domain.Entities;

[Table("mentorias")]
public class Mentoria : BaseModel
{
    [PrimaryKey("id")]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Column("nome")]
    public string Nome { get; set; } = string.Empty;
    
    [Column("mentor_id")]
    public Guid MentorId { get; set; }
    
    [Column("assistant_id")]
    public string AssistantId { get; set; } = string.Empty;
    
    [Column("duracao_dias")]
    public int DuracaoDias { get; set; }
    
    [Column("descricao")]
    public string? Descricao { get; set; }
    
    [Column("status")]
    public MentoriaStatus Status { get; set; } = MentoriaStatus.Active;
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

