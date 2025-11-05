using FluentValidation;
using Mentoragente.Domain.DTOs;

namespace Mentoragente.Application.Validators;

public class CreateMentoriaRequestValidator : AbstractValidator<CreateMentoriaRequestDto>
{
    public CreateMentoriaRequestValidator()
    {
        RuleFor(x => x.MentorId)
            .NotEmpty().WithMessage("Mentor ID is required");

        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome is required")
            .MinimumLength(3).WithMessage("Nome must be at least 3 characters")
            .MaximumLength(200).WithMessage("Nome cannot exceed 200 characters");

        RuleFor(x => x.AssistantId)
            .NotEmpty().WithMessage("Assistant ID is required")
            .MaximumLength(100).WithMessage("Assistant ID cannot exceed 100 characters");

        RuleFor(x => x.DuracaoDias)
            .GreaterThan(0).WithMessage("Duração em dias must be greater than 0")
            .LessThanOrEqualTo(365).WithMessage("Duração em dias cannot exceed 365 days");

        RuleFor(x => x.Descricao)
            .MaximumLength(1000).WithMessage("Descrição cannot exceed 1000 characters")
            .When(x => !string.IsNullOrEmpty(x.Descricao));
    }
}

public class UpdateMentoriaRequestValidator : AbstractValidator<UpdateMentoriaRequestDto>
{
    public UpdateMentoriaRequestValidator()
    {
        RuleFor(x => x.Nome)
            .MinimumLength(3).WithMessage("Nome must be at least 3 characters")
            .MaximumLength(200).WithMessage("Nome cannot exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.Nome));

        RuleFor(x => x.AssistantId)
            .MaximumLength(100).WithMessage("Assistant ID cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.AssistantId));

        RuleFor(x => x.DuracaoDias)
            .GreaterThan(0).WithMessage("Duração em dias must be greater than 0")
            .LessThanOrEqualTo(365).WithMessage("Duração em dias cannot exceed 365 days")
            .When(x => x.DuracaoDias.HasValue);

        RuleFor(x => x.Descricao)
            .MaximumLength(1000).WithMessage("Descrição cannot exceed 1000 characters")
            .When(x => !string.IsNullOrEmpty(x.Descricao));

        RuleFor(x => x.Status)
            .Must(BeValidStatus).WithMessage("Status must be one of: Active, Inactive, Archived")
            .When(x => !string.IsNullOrEmpty(x.Status));
    }

    private bool BeValidStatus(string? status)
    {
        if (string.IsNullOrEmpty(status)) return true;
        return status == "Active" || status == "Inactive" || status == "Archived";
    }
}

