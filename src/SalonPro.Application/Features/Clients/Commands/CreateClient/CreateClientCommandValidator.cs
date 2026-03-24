using FluentValidation;

namespace SalonPro.Application.Features.Clients.Commands.CreateClient;

public class CreateClientCommandValidator : AbstractValidator<CreateClientCommand>
{
    public CreateClientCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Ime je obavezno.")
            .MaximumLength(100).WithMessage("Ime ne sme biti duže od 100 karaktera.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Prezime je obavezno.")
            .MaximumLength(100).WithMessage("Prezime ne sme biti duže od 100 karaktera.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Telefon je obavezan.")
            .MaximumLength(20).WithMessage("Telefon ne sme biti duži od 20 karaktera.");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Unesite ispravnu email adresu.")
            .MaximumLength(256).WithMessage("Email ne sme biti duži od 256 karaktera.")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Napomene ne smeju biti duže od 1000 karaktera.")
            .When(x => x.Notes != null);

        RuleFor(x => x.Tags)
            .MaximumLength(500).WithMessage("Oznake ne smeju biti duže od 500 karaktera.")
            .When(x => x.Tags != null);
    }
}
