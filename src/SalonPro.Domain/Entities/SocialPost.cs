using SalonPro.Domain.Common;
using SalonPro.Domain.Enums;

namespace SalonPro.Domain.Entities;

public class SocialPost : BaseAuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public string Topic { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
    public string Hashtags { get; set; } = string.Empty;
    public string? ImagePrompt { get; set; }
    public string? ImageUrl { get; set; }
    public string? InstagramMediaId { get; set; }
    public SocialPostStatus Status { get; set; } = SocialPostStatus.Draft;
    public DateTime? PublishedAt { get; set; }
    public string? FailureReason { get; set; }

    public Tenant Tenant { get; set; } = null!;
}
