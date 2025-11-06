namespace Mentoragente.Domain.DTOs;

public class AgentSessionResponseDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid MentorshipId { get; set; }
    public string AIProvider { get; set; } = string.Empty;
    public string? AIContextId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? LastInteraction { get; set; }
    public int TotalMessages { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateAgentSessionRequestDto
{
    public Guid UserId { get; set; }
    public Guid MentorshipId { get; set; }
    public string? AIContextId { get; set; }
}

public class UpdateAgentSessionRequestDto
{
    public string? Status { get; set; }
    public string? AIContextId { get; set; }
    public DateTime? LastInteraction { get; set; }
}

public class AgentSessionListResponseDto
{
    public List<AgentSessionResponseDto> Sessions { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class PaginationRequestDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    public int Skip => (Page - 1) * PageSize;
    public int Take => PageSize;
}

