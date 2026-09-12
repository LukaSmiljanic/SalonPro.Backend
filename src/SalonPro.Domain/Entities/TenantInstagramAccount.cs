using SalonPro.Domain.Common;

namespace SalonPro.Domain.Entities;

public class TenantInstagramAccount : BaseAuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string FacebookPageId { get; set; } = string.Empty;
    public string InstagramUserId { get; set; } = string.Empty;
    public string? InstagramUsername { get; set; }
    /// <summary>Protected page access token (long-lived).</summary>
    public string ProtectedAccessToken { get; set; } = string.Empty;
    public DateTime? TokenExpiresAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
}
