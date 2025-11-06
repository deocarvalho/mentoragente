namespace Mentoragente.Domain.DTOs;

public class MentorshipResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid MentorId { get; set; }
    public string AssistantId { get; set; } = string.Empty;
    public int DurationDays { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public string EvolutionApiKey { get; set; } = string.Empty;
    public string EvolutionInstanceName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateMentorshipRequestDto
{
    public Guid MentorId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AssistantId { get; set; } = string.Empty;
    public int DurationDays { get; set; }
    public string? Description { get; set; }
    public string EvolutionApiKey { get; set; } = string.Empty;
    public string EvolutionInstanceName { get; set; } = string.Empty;
}

public class UpdateMentorshipRequestDto
{
    public string? Name { get; set; }
    public string? AssistantId { get; set; }
    public int? DurationDays { get; set; }
    public string? Description { get; set; }
    public string? Status { get; set; }
    public string? EvolutionApiKey { get; set; }
    public string? EvolutionInstanceName { get; set; }
}

public class MentorshipListResponseDto
{
    public List<MentorshipResponseDto> Mentorships { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

