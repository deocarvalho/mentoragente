using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using Mentoragente.Domain.Enums;
using Newtonsoft.Json;

namespace Mentoragente.Domain.Entities;

[Table("mentorships")]
public class Mentorship : BaseModel
{
    [PrimaryKey("id")]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Column("name")]
    public string Name { get; set; } = string.Empty;
    
    [Column("mentor_id")]
    public Guid MentorId { get; set; }
    
    [Column("assistant_id")]
    public string AssistantId { get; set; } = string.Empty;
    
    [Column("duration_days")]
    public int DurationDays { get; set; }
    
    [Column("description")]
    public string? Description { get; set; }
    
    [Column("status")]
    public MentorshipStatus Status { get; set; } = MentorshipStatus.Active;
    
    [Column("evolution_api_key")]
    public string EvolutionApiKey { get; set; } = string.Empty;
    
    [Column("evolution_instance_name")]
    public string EvolutionInstanceName { get; set; } = string.Empty;
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

