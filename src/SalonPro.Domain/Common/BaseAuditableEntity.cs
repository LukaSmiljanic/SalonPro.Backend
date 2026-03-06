namespace SalonPro.Domain.Common;

public abstract class BaseAuditableEntity : BaseEntity
{
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    /// <summary>Alias for UpdatedAt for compatibility.</summary>
    public DateTime? LastModifiedAt { get => UpdatedAt; set => UpdatedAt = value; }
}
