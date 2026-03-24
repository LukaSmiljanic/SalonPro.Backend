using FluentValidation;

namespace SalonPro.Application.Features.Payments.Commands.CreatePayment;

public class CreatePaymentCommandValidator : AbstractValidator<CreatePaymentCommand>
{
    public CreatePaymentCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("ID salona je obavezan.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Iznos mora biti veći od 0.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Valuta je obavezna.")
            .MaximumLength(10).WithMessage("Valuta ne sme biti duža od 10 karaktera.");

        RuleFor(x => x.PeriodStart)
            .NotEmpty().WithMessage("Početak perioda je obavezan.");

        RuleFor(x => x.PeriodEnd)
            .NotEmpty().WithMessage("Kraj perioda je obavezan.")
            .GreaterThan(x => x.PeriodStart).WithMessage("Kraj perioda mora biti posle početka.");

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Napomene ne smeju biti duže od 2000 karaktera.")
            .When(x => x.Notes != null);
    }
}
