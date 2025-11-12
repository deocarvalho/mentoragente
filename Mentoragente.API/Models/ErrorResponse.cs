namespace Mentoragente.API.Models;

/// <summary>
/// Standardized error response model for API errors
/// </summary>
public class ErrorResponse
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Status { get; set; }
    public string? Detail { get; set; }
    public string? TraceId { get; set; }
    public Dictionary<string, object>? Extensions { get; set; }
}



