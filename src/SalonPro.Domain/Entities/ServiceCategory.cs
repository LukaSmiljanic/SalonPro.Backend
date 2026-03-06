using SalonPro.Domain.Common;
using SalonPro.Domain.Enums;

namespace SalonPro.Domain.Entities;

public class ServiceCategory : BaseAuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Color { get; set; }
    public ServiceCategoryType Type { get; set; } = ServiceCategoryType.Other;
    public bool IsActive { get; set; } = true;

    /// <summary>Alias for Color (hex format).</summary>
    public string? ColorHex { get => Color; set => Color = value; }

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public ICollection<Service> Services { get; set; } = new List<Service>();
}
