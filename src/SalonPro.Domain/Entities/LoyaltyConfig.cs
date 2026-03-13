using SalonPro.Domain.Common;

namespace SalonPro.Domain.Entities;

public class LoyaltyConfig : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string TierName { get; set; } = string.Empty; // Bronze, Silver, Gold, Platinum
    public int MinVisits { get; set; }
    public string Benefit { get; set; } = string.Empty; // e.g. "20% popusta"
    public Tenant Tenant { get; set; } = null!;
}
