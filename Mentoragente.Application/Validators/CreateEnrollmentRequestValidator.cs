using FluentValidation;
using Mentoragente.Domain.DTOs;

namespace Mentoragente.Application.Validators;

public class CreateEnrollmentRequestValidator : AbstractValidator<CreateEnrollmentRequestDto>
{
    public CreateEnrollmentRequestValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required")
            .Matches(@"^\d{10,15}$").WithMessage("Phone number must contain only digits and be between 10 and 15 characters")
            .MaximumLength(15).WithMessage("Phone number cannot exceed 15 characters");

        RuleFor(x => x.MentorshipId)
            .NotEmpty().WithMessage("Mentorship ID is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(255).WithMessage("Email cannot exceed 255 characters")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.PurchaseId)
            .MaximumLength(100).WithMessage("Purchase ID cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.PurchaseId));
    }
}

