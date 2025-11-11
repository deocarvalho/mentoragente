namespace Mentoragente.Domain.Models;

/// <summary>
/// Generic WhatsApp message model - provider agnostic
/// Used internally after converting provider-specific webhook DTOs
/// </summary>
public class WhatsAppMessage
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string MessageText { get; set; } = string.Empty;
    public bool FromMe { get; set; }
    public string? MessageId { get; set; }
    public DateTime? Timestamp { get; set; }
    public string? ContactName { get; set; }
    public bool IsGroup { get; set; }
}

