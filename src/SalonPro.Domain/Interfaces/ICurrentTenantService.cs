namespace SalonPro.Domain.Interfaces;

public interface ICurrentTenantService
{
    Guid? TenantId { get; }
    string? TenantSlug { get; }
}
