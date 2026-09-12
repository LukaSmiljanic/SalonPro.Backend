using MediatR;

namespace SalonPro.Application.Features.Tenants.Commands.ProvisionDemoTenant;

public record ProvisionDemoTenantCommand(
    string Email,
    string FirstName,
    string LastName,
    string TenantName,
    string TenantSlug,
    string? City = null,
    /// <summary>If set, used instead of a random temporary password (min. 8 characters).</summary>
    string? Password = null
) : IRequest<ProvisionDemoTenantResult>;

public record ProvisionDemoTenantResult(
    Guid TenantId,
    string LoginEmail,
    string TemporaryPassword,
    DateTime SubscriptionEndsAtUtc,
    int TrialDays
);
