using FluentValidation;

namespace SalonPro.Application.Features.Payments.Commands.CreatePayment;

public class CreatePaymentCommandValidator : AbstractValidator<CreatePaymentCommand>
{
    public CreatePaymentCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("TenantId is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than 0.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .MaximumLength(10).WithMessage("Currency must not exceed 10 characters.");

        RuleFor(x => x.PeriodStart)
            .NotEmpty().WithMessage("PeriodStart is required.");

        RuleFor(x => x.PeriodEnd)
            .NotEmpty().WithMessage("PeriodEnd is required.")
            .GreaterThan(x => x.PeriodStart).WithMessage("PeriodEnd must be after PeriodStart.");

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Notes must not exceed 2000 characters.")
            .When(x => x.Notes != null);
    }
}
