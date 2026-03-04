using SalonPro.Domain.Interfaces;

namespace SalonPro.Infrastructure.Services;

public class CurrentTenantService : ICurrentTenantService
{
    public Guid? TenantId { get; set; }
    public string? TenantSlug { get; set; }
}
