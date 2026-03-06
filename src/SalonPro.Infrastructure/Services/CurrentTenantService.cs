using SalonPro.Domain.Interfaces;

namespace SalonPro.Infrastructure.Services;

public class CurrentTenantService : ICurrentTenantService
{
    public Guid? TenantId { get; private set; }
    public string? TenantSlug { get; set; }

    public void SetTenant(Guid tenantId) => TenantId = tenantId;
}
