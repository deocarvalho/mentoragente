using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Mentoragente.Domain.Entities;

[Table("agent_session_data")]
public class AgentSessionData : BaseModel
{
    [PrimaryKey("agent_session_id")]
    public Guid AgentSessionId { get; set; }
    
    [Column("access_start_date")]
    public DateTime AccessStartDate { get; set; }
    
    [Column("access_end_date")]
    public DateTime AccessEndDate { get; set; }
    
    [Column("progress_percentage")]
    public int ProgressPercentage { get; set; } = 0;
    
    [Column("report_generated")]
    public bool ReportGenerated { get; set; } = false;
    
    [Column("report_generated_at")]
    public DateTime? ReportGeneratedAt { get; set; }
    
    [Column("admin_notes")]
    public string? AdminNotes { get; set; }
    
    [Column("custom_properties_json")]
    public string? CustomPropertiesJson { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

