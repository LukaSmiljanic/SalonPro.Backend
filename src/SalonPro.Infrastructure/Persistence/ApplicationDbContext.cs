using System.Reflection;
using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Domain.Common;
using SalonPro.Domain.Entities;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly ICurrentTenantService _currentTenantService;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentTenantService currentTenantService)
        : base(options)
    {
        _currentTenantService = currentTenantService;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<ServiceCategory> ServiceCategories => Set<ServiceCategory>();
    public DbSet<StaffMember> StaffMembers => Set<StaffMember>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<AppointmentService> AppointmentServices => Set<AppointmentService>();
    public DbSet<ClientNote> ClientNotes => Set<ClientNote>();
    public DbSet<WorkingHours> WorkingHours => Set<WorkingHours>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<LoyaltyConfig> LoyaltyConfigs => Set<LoyaltyConfig>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply all IEntityTypeConfiguration<T> from this assembly
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Apply global query filters for multi-tenancy on all ITenantEntity implementations
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType)
                && entityType.ClrType != typeof(Tenant))
            {
                var method = typeof(ApplicationDbContext)
                    .GetMethod(nameof(ApplyTenantFilter), BindingFlags.NonPublic | BindingFlags.Instance)!
                    .MakeGenericMethod(entityType.ClrType);

                method.Invoke(this, new object[] { builder });
            }
        }
    }

    private void ApplyTenantFilter<TEntity>(ModelBuilder builder)
        where TEntity : class, ITenantEntity
    {
        builder.Entity<TEntity>().HasQueryFilter(
            e => _currentTenantService.TenantId == null ||
                 e.TenantId == _currentTenantService.TenantId);
    }
}
