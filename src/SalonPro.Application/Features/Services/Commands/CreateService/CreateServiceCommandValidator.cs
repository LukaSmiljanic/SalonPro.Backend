using FluentValidation;

namespace SalonPro.Application.Features.Services.Commands.CreateService;

public class CreateServiceCommandValidator : AbstractValidator<CreateServiceCommand>
{
    public CreateServiceCommandValidator()
    {
        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Kategorija je obavezna.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Naziv usluge je obavezan.")
            .MaximumLength(200).WithMessage("Naziv usluge ne sme biti duži od 200 karaktera.");

        RuleFor(x => x.DurationMinutes)
            .GreaterThan(0).WithMessage("Trajanje mora biti veće od 0 minuta.")
            .LessThanOrEqualTo(480).WithMessage("Trajanje ne sme preći 480 minuta (8 sati).");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Cena mora biti 0 ili veća.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Opis ne sme biti duži od 1000 karaktera.")
            .When(x => x.Description != null);
    }
}
