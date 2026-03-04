using SalonPro.Domain.Common;

namespace SalonPro.Domain.Entities;

public class WorkingHours : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid? StaffMemberId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsWorkingDay { get; set; } = true;

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public StaffMember? StaffMember { get; set; }
}
