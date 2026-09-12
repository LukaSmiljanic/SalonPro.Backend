using MediatR;

namespace SalonPro.Application.Features.Tenants.Commands.ActivateTenantAccess;

public record ActivateTenantAccessCommand(Guid TenantId, int TrialDays = 30) : IRequest<ActivateTenantAccessResult>;

public record ActivateTenantAccessResult(
    bool Success,
    string Message,
    DateTime? SubscriptionEndDateUtc);
