using SalonPro.Domain.Common;

namespace SalonPro.Domain.Entities;

public class SocialGalleryImage : BaseAuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = "image/jpeg";
    public long FileSizeBytes { get; set; }
    public string? CaptionHint { get; set; }

    public Tenant Tenant { get; set; } = null!;
}
