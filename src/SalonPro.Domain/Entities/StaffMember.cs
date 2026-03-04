using SalonPro.Domain.Common;

namespace SalonPro.Domain.Entities;

public class StaffMember : BaseAuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid? UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Title { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public int ColorIndex { get; set; } = 0;

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public User? User { get; set; }
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<WorkingHours> WorkingHours { get; set; } = new List<WorkingHours>();
}
