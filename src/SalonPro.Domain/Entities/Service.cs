using SalonPro.Domain.Common;

namespace SalonPro.Domain.Entities;

public class Service : BaseAuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public ServiceCategory Category { get; set; } = null!;
    public ICollection<AppointmentService> AppointmentServices { get; set; } = new List<AppointmentService>();
}
