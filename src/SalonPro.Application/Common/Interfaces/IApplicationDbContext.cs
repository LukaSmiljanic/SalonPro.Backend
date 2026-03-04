using Microsoft.EntityFrameworkCore;
using SalonPro.Domain.Entities;

namespace SalonPro.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<User> Users { get; }
    DbSet<Client> Clients { get; }
    DbSet<ServiceCategory> ServiceCategories { get; }
    DbSet<Service> Services { get; }
    DbSet<StaffMember> StaffMembers { get; }
    DbSet<Appointment> Appointments { get; }
    DbSet<AppointmentService> AppointmentServices { get; }
    DbSet<ClientNote> ClientNotes { get; }
    DbSet<WorkingHours> WorkingHours { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
