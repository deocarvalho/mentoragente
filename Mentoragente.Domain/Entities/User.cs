using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using Mentoragente.Domain.Enums;

namespace Mentoragente.Domain.Entities;

[Table("users")]
public class User : BaseModel
{
    [PrimaryKey("id")]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Column("phone_number")]
    public string PhoneNumber { get; set; } = string.Empty;
    
    [Column("name")]
    public string Name { get; set; } = string.Empty;
    
    [Column("email")]
    public string? Email { get; set; }
    
    [Column("status")]
    public UserStatus Status { get; set; } = UserStatus.Active;
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

