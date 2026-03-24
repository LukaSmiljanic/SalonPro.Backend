using FluentValidation;

namespace SalonPro.Application.Features.Staff.Commands.CreateStaffMember;

public class CreateStaffMemberCommandValidator : AbstractValidator<CreateStaffMemberCommand>
{
    public CreateStaffMemberCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Ime je obavezno.")
            .MaximumLength(100).WithMessage("Ime ne sme imati više od 100 karaktera.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Prezime je obavezno.")
            .MaximumLength(100).WithMessage("Prezime ne sme imati više od 100 karaktera.");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Unesite validnu email adresu.")
            .MaximumLength(256).WithMessage("Email ne sme imati više od 256 karaktera.")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("Telefon ne sme imati više od 20 karaktera.")
            .When(x => x.Phone != null);

        RuleFor(x => x.Specialization)
            .MaximumLength(200).WithMessage("Specijalizacija ne sme imati više od 200 karaktera.")
            .When(x => x.Specialization != null);
    }
}
