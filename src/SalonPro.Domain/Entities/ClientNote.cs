using SalonPro.Domain.Common;

namespace SalonPro.Domain.Entities;

public class ClientNote : BaseAuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid ClientId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsPinned { get; set; } = false;

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public Client Client { get; set; } = null!;
}
