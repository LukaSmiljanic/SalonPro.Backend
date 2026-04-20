using FluentValidation;
using SalonPro.Application.Common;

namespace SalonPro.Application.Features.Tenants.Commands.UpdateTenantPlan;

public class UpdateTenantPlanCommandValidator : AbstractValidator<UpdateTenantPlanCommand>
{
    public UpdateTenantPlanCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Plan)
            .NotEmpty()
            .Must(plan =>
                string.Equals(plan, TenantPlanRules.Basic, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(plan, TenantPlanRules.Standard, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(plan, TenantPlanRules.Pro, StringComparison.OrdinalIgnoreCase))
            .WithMessage("Plan mora biti Basic, Standard ili Pro.");
    }
}

