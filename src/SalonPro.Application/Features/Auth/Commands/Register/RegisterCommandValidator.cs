using FluentValidation;

namespace SalonPro.Application.Features.Auth.Commands.Register;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.TenantName)
            .NotEmpty().WithMessage("Salon name is required.")
            .MaximumLength(100).WithMessage("Salon name cannot exceed 100 characters.");

        RuleFor(x => x.TenantSlug)
            .NotEmpty().WithMessage("Salon slug is required.")
            .MaximumLength(50).WithMessage("Salon slug cannot exceed 50 characters.")
            .Matches(@"^[a-z0-9-]+$").WithMessage("Salon slug can only contain lowercase letters, numbers, and hyphens.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one number.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(50).WithMessage("First name cannot exceed 50 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(50).WithMessage("Last name cannot exceed 50 characters.");
    }
}
