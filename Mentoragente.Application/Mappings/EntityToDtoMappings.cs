using Mentoragente.Domain.DTOs;
using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Enums;

namespace Mentoragente.Application.Mappings;

public static class EntityToDtoMappings
{
    public static UserResponseDto ToDto(this User user)
    {
        return new UserResponseDto
        {
            Id = user.Id,
            PhoneNumber = user.PhoneNumber,
            Name = user.Name,
            Email = user.Email,
            Status = user.Status.ToString(),
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    public static MentoriaResponseDto ToDto(this Mentoria mentoria)
    {
        return new MentoriaResponseDto
        {
            Id = mentoria.Id,
            Nome = mentoria.Nome,
            MentorId = mentoria.MentorId,
            AssistantId = mentoria.AssistantId,
            DuracaoDias = mentoria.DuracaoDias,
            Descricao = mentoria.Descricao,
            Status = mentoria.Status.ToString(),
            CreatedAt = mentoria.CreatedAt,
            UpdatedAt = mentoria.UpdatedAt
        };
    }

    public static AgentSessionResponseDto ToDto(this AgentSession session)
    {
        return new AgentSessionResponseDto
        {
            Id = session.Id,
            UserId = session.UserId,
            MentoriaId = session.MentoriaId,
            AIProvider = session.AIProvider.ToString(),
            AIContextId = session.AIContextId,
            Status = session.Status.ToString(),
            LastInteraction = session.LastInteraction,
            TotalMessages = session.TotalMessages,
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt
        };
    }

    public static UserStatus? ParseUserStatus(string? status)
    {
        if (string.IsNullOrEmpty(status)) return null;
        return Enum.TryParse<UserStatus>(status, true, out var result) ? result : null;
    }

    public static MentoriaStatus? ParseMentoriaStatus(string? status)
    {
        if (string.IsNullOrEmpty(status)) return null;
        return Enum.TryParse<MentoriaStatus>(status, true, out var result) ? result : null;
    }

    public static AgentSessionStatus? ParseAgentSessionStatus(string? status)
    {
        if (string.IsNullOrEmpty(status)) return null;
        return Enum.TryParse<AgentSessionStatus>(status, true, out var result) ? result : null;
    }
}

