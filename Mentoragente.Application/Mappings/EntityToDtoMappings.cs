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

    public static MentorshipResponseDto ToDto(this Mentorship mentorship)
    {
        return new MentorshipResponseDto
        {
            Id = mentorship.Id,
            Name = mentorship.Name,
            MentorId = mentorship.MentorId,
            AssistantId = mentorship.AssistantId,
            DurationDays = mentorship.DurationDays,
            Description = mentorship.Description,
            Status = mentorship.Status.ToString(),
            EvolutionApiKey = mentorship.EvolutionApiKey,
            EvolutionInstanceName = mentorship.EvolutionInstanceName,
            CreatedAt = mentorship.CreatedAt,
            UpdatedAt = mentorship.UpdatedAt
        };
    }

    public static AgentSessionResponseDto ToDto(this AgentSession session)
    {
        return new AgentSessionResponseDto
        {
            Id = session.Id,
            UserId = session.UserId,
            MentorshipId = session.MentorshipId,
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

    public static MentorshipStatus? ParseMentorshipStatus(string? status)
    {
        if (string.IsNullOrEmpty(status)) return null;
        return Enum.TryParse<MentorshipStatus>(status, true, out var result) ? result : null;
    }

    public static AgentSessionStatus? ParseAgentSessionStatus(string? status)
    {
        if (string.IsNullOrEmpty(status)) return null;
        return Enum.TryParse<AgentSessionStatus>(status, true, out var result) ? result : null;
    }
}

