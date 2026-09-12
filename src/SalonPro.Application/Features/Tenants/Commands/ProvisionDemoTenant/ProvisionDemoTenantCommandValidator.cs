using FluentValidation;
using System.Text.RegularExpressions;

namespace SalonPro.Application.Features.Tenants.Commands.ProvisionDemoTenant;

public partial class ProvisionDemoTenantCommandValidator : AbstractValidator<ProvisionDemoTenantCommand>
{
    public ProvisionDemoTenantCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.TenantName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TenantSlug)
            .NotEmpty()
            .MaximumLength(80)
            .Must(s => SlugRegex().IsMatch(s))
            .WithMessage("Slug sme da sadrži samo mala slova, brojeve i crtice (npr. salon-lepota-ruma).");
        RuleFor(x => x.City).MaximumLength(100).When(x => x.City != null);
        RuleFor(x => x.Password)
            .MinimumLength(8).WithMessage("Lozinka mora imati najmanje 8 karaktera.")
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.Password));
    }

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    private static partial Regex SlugRegex();
}
