using FluentValidation;
using Mentoragente.Domain.DTOs;

namespace Mentoragente.Application.Validators;

public class CreateAgentSessionRequestValidator : AbstractValidator<CreateAgentSessionRequestDto>
{
    public CreateAgentSessionRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.MentorshipId)
            .NotEmpty().WithMessage("Mentorship ID is required");

        RuleFor(x => x.AIContextId)
            .MaximumLength(200).WithMessage("AI Context ID cannot exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.AIContextId));
    }
}

public class UpdateAgentSessionRequestValidator : AbstractValidator<UpdateAgentSessionRequestDto>
{
    public UpdateAgentSessionRequestValidator()
    {
        RuleFor(x => x.Status)
            .Must(BeValidStatus).WithMessage("Status must be one of: Active, Expired, Paused, Completed")
            .When(x => !string.IsNullOrEmpty(x.Status));

        RuleFor(x => x.AIContextId)
            .MaximumLength(200).WithMessage("AI Context ID cannot exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.AIContextId));
    }

    private bool BeValidStatus(string? status)
    {
        if (string.IsNullOrEmpty(status)) return true;
        return status == "Active" || status == "Expired" || status == "Paused" || status == "Completed";
    }
}

public class PaginationRequestValidator : AbstractValidator<PaginationRequestDto>
{
    public PaginationRequestValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Page must be greater than 0")
            .LessThanOrEqualTo(1000).WithMessage("Page cannot exceed 1000");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("Page size must be greater than 0")
            .LessThanOrEqualTo(100).WithMessage("Page size cannot exceed 100");
    }
}

