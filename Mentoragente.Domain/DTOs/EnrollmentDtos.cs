namespace Mentoragente.Domain.DTOs;

public class CreateEnrollmentRequestDto
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public Guid MentorshipId { get; set; }
    public string? PurchaseId { get; set; }
    public DateTime? PurchaseDate { get; set; }
}

public class EnrollmentResponseDto
{
    public bool Success { get; set; }
    public Guid SessionId { get; set; }
    public bool WelcomeMessageSent { get; set; }
    public string Message { get; set; } = string.Empty;
}

