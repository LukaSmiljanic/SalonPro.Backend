using System.ComponentModel.DataAnnotations.Schema;
using SalonPro.Domain.Common;

namespace SalonPro.Domain.Entities;

public class Tenant : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? LogoUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public string? TimeZone { get; set; }
    public string? Currency { get; set; } = "RSD";
    public string? Language { get; set; } = "sr";

    // Email verification
    public bool EmailVerified { get; set; } = false;
    public string? EmailVerificationToken { get; set; }
    public DateTime? EmailVerificationTokenExpiry { get; set; }

    // Subscription
    public DateTime? SubscriptionStartDate { get; set; }
    public DateTime? SubscriptionEndDate { get; set; }
    public bool IsTrialing { get; set; } = false;

    /// <summary>True if subscription (or trial) is currently valid.</summary>
    [NotMapped]
    public bool HasActiveSubscription =>
        SubscriptionEndDate.HasValue && SubscriptionEndDate.Value > DateTime.UtcNow;

    // Navigation
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Client> Clients { get; set; } = new List<Client>();
    public ICollection<ServiceCategory> ServiceCategories { get; set; } = new List<ServiceCategory>();
    public ICollection<StaffMember> StaffMembers { get; set; } = new List<StaffMember>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<WorkingHours> WorkingHours { get; set; } = new List<WorkingHours>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
