using FluentValidation;

namespace SalonPro.Application.Features.Auth.Commands.Register;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.TenantName)
            .NotEmpty().WithMessage("Naziv salona je obavezan.")
            .MaximumLength(100).WithMessage("Naziv salona ne sme biti duži od 100 karaktera.");

        RuleFor(x => x.TenantSlug)
            .NotEmpty().WithMessage("URL identifikator salona je obavezan.")
            .MaximumLength(50).WithMessage("URL identifikator ne sme biti duži od 50 karaktera.")
            .Matches(@"^[a-z0-9-]+$").WithMessage("URL identifikator može sadržati samo mala slova, brojeve i crtice.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email je obavezan.")
            .EmailAddress().WithMessage("Unesite ispravnu email adresu.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Lozinka je obavezna.")
            .MinimumLength(8).WithMessage("Lozinka mora imati najmanje 8 karaktera.")
            .Matches(@"[A-Z]").WithMessage("Lozinka mora sadržati bar jedno veliko slovo.")
            .Matches(@"[a-z]").WithMessage("Lozinka mora sadržati bar jedno malo slovo.")
            .Matches(@"[0-9]").WithMessage("Lozinka mora sadržati bar jedan broj.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Ime je obavezno.")
            .MaximumLength(50).WithMessage("Ime ne sme biti duže od 50 karaktera.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Prezime je obavezno.")
            .MaximumLength(50).WithMessage("Prezime ne sme biti duže od 50 karaktera.");
    }
}
