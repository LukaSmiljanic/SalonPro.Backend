using SalonPro.Domain.Common;
using SalonPro.Domain.Enums;

namespace SalonPro.Domain.Entities;

public class Appointment : BaseAuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid ClientId { get; set; }
    public Guid StaffMemberId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
    public string? Notes { get; set; }
    public decimal TotalPrice { get; set; }
    public int TotalDurationMinutes { get; set; }

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public Client Client { get; set; } = null!;
    public StaffMember StaffMember { get; set; } = null!;
    public ICollection<AppointmentService> AppointmentServices { get; set; } = new List<AppointmentService>();
}
