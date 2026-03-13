using SalonPro.Domain.Entities;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public IRepository<Tenant> Tenants { get; }
    public IRepository<User> Users { get; }
    public IRepository<Client> Clients { get; }
    public IRepository<ServiceCategory> ServiceCategories { get; }
    public IRepository<Service> Services { get; }
    public IRepository<StaffMember> StaffMembers { get; }
    public IRepository<Appointment> Appointments { get; }
    public IRepository<AppointmentService> AppointmentServices { get; }
    public IRepository<ClientNote> ClientNotes { get; }
    public IRepository<WorkingHours> WorkingHours { get; }
    public IRepository<Payment> Payments { get; }
    public IRepository<LoyaltyConfig> LoyaltyConfigs { get; }

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
        Tenants = new Repository<Tenant>(context);
        Users = new Repository<User>(context);
        Clients = new Repository<Client>(context);
        ServiceCategories = new Repository<ServiceCategory>(context);
        Services = new Repository<Service>(context);
        StaffMembers = new Repository<StaffMember>(context);
        Appointments = new Repository<Appointment>(context);
        AppointmentServices = new Repository<AppointmentService>(context);
        ClientNotes = new Repository<ClientNote>(context);
        WorkingHours = new Repository<WorkingHours>(context);
        Payments = new Repository<Payment>(context);
        LoyaltyConfigs = new Repository<LoyaltyConfig>(context);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);

    public void Dispose() => _context.Dispose();
}
