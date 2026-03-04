using SalonPro.Domain.Common;

namespace SalonPro.Domain.Entities;

public class Client : BaseAuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<ClientNote> ClientNotes { get; set; } = new List<ClientNote>();
}
