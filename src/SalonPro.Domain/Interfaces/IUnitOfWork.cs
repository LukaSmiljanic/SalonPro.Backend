using SalonPro.Domain.Entities;

namespace SalonPro.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRepository<Tenant> Tenants { get; }
    IRepository<User> Users { get; }
    IRepository<Client> Clients { get; }
    IRepository<ServiceCategory> ServiceCategories { get; }
    IRepository<Service> Services { get; }
    IRepository<StaffMember> StaffMembers { get; }
    IRepository<Appointment> Appointments { get; }
    IRepository<AppointmentService> AppointmentServices { get; }
    IRepository<ClientNote> ClientNotes { get; }
    IRepository<WorkingHours> WorkingHours { get; }
    IRepository<Payment> Payments { get; }
    IRepository<LoyaltyConfig> LoyaltyConfigs { get; }
    IRepository<SocialPost> SocialPosts { get; }
    IRepository<TenantInstagramAccount> TenantInstagramAccounts { get; }
    IRepository<SocialGalleryImage> SocialGalleryImages { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
