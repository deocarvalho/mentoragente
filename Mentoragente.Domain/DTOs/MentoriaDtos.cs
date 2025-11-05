namespace Mentoragente.Domain.DTOs;

public class MentoriaResponseDto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public Guid MentorId { get; set; }
    public string AssistantId { get; set; } = string.Empty;
    public int DuracaoDias { get; set; }
    public string? Descricao { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateMentoriaRequestDto
{
    public Guid MentorId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string AssistantId { get; set; } = string.Empty;
    public int DuracaoDias { get; set; }
    public string? Descricao { get; set; }
}

public class UpdateMentoriaRequestDto
{
    public string? Nome { get; set; }
    public string? AssistantId { get; set; }
    public int? DuracaoDias { get; set; }
    public string? Descricao { get; set; }
    public string? Status { get; set; }
}

public class MentoriaListResponseDto
{
    public List<MentoriaResponseDto> Mentorias { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

