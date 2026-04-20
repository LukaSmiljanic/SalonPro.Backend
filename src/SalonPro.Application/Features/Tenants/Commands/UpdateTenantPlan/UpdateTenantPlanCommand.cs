using MediatR;

namespace SalonPro.Application.Features.Tenants.Commands.UpdateTenantPlan;

public record UpdateTenantPlanCommand(
    Guid TenantId,
    string Plan
) : IRequest<Unit>;

