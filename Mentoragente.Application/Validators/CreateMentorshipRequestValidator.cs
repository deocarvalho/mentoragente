using FluentValidation;
using Mentoragente.Domain.DTOs;

namespace Mentoragente.Application.Validators;

public class CreateMentorshipRequestValidator : AbstractValidator<CreateMentorshipRequestDto>
{
    public CreateMentorshipRequestValidator()
    {
        RuleFor(x => x.MentorId)
            .NotEmpty().WithMessage("Mentor ID is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MinimumLength(3).WithMessage("Name must be at least 3 characters")
            .MaximumLength(200).WithMessage("Name cannot exceed 200 characters");

        RuleFor(x => x.AssistantId)
            .NotEmpty().WithMessage("Assistant ID is required")
            .MaximumLength(100).WithMessage("Assistant ID cannot exceed 100 characters");

        RuleFor(x => x.DurationDays)
            .GreaterThan(0).WithMessage("Duration in days must be greater than 0")
            .LessThanOrEqualTo(365).WithMessage("Duration in days cannot exceed 365 days");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.InstanceCode)
            .NotEmpty().WithMessage("Instance code is required")
            .MaximumLength(100).WithMessage("Instance code cannot exceed 100 characters");

        RuleFor(x => x.WhatsAppProvider)
            .Must(BeValidProvider).WithMessage("WhatsApp Provider must be one of: EvolutionAPI, ZApi, OfficialWhatsApp")
            .When(x => !string.IsNullOrEmpty(x.WhatsAppProvider));
    }
}

public class UpdateMentorshipRequestValidator : AbstractValidator<UpdateMentorshipRequestDto>
{
    public UpdateMentorshipRequestValidator()
    {
        RuleFor(x => x.Name)
            .MinimumLength(3).WithMessage("Name must be at least 3 characters")
            .MaximumLength(200).WithMessage("Name cannot exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.Name));

        RuleFor(x => x.AssistantId)
            .MaximumLength(100).WithMessage("Assistant ID cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.AssistantId));

        RuleFor(x => x.DurationDays)
            .GreaterThan(0).WithMessage("Duration in days must be greater than 0")
            .LessThanOrEqualTo(365).WithMessage("Duration in days cannot exceed 365 days")
            .When(x => x.DurationDays.HasValue);

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Status)
            .Must(BeValidStatus).WithMessage("Status must be one of: Active, Inactive, Archived")
            .When(x => !string.IsNullOrEmpty(x.Status));

        RuleFor(x => x.InstanceCode)
            .MaximumLength(100).WithMessage("Instance code cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.InstanceCode));

        RuleFor(x => x.WhatsAppProvider)
            .Must(BeValidProvider).WithMessage("WhatsApp Provider must be one of: EvolutionAPI, ZApi, OfficialWhatsApp")
            .When(x => !string.IsNullOrEmpty(x.WhatsAppProvider));
    }

    private bool BeValidStatus(string? status)
    {
        if (string.IsNullOrEmpty(status)) return true;
        return status == "Active" || status == "Inactive" || status == "Archived";
    }

    private bool BeValidProvider(string? provider)
    {
        if (string.IsNullOrEmpty(provider)) return true;
        return provider == "EvolutionAPI" || provider == "ZApi" || provider == "OfficialWhatsApp";
    }
}

