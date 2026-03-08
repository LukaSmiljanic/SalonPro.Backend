using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalonPro.Domain.Entities;

namespace SalonPro.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(200);
        builder.Property(t => t.Slug).IsRequired().HasMaxLength(100);
        builder.HasIndex(t => t.Slug).IsUnique();
        builder.Property(t => t.Email).HasMaxLength(256);
        builder.Property(t => t.Phone).HasMaxLength(50);
        builder.Property(t => t.Address).HasMaxLength(500);
        builder.Property(t => t.City).HasMaxLength(100);
        builder.Property(t => t.Country).HasMaxLength(100);
        builder.Property(t => t.LogoUrl).HasMaxLength(2000);
        builder.Property(t => t.TimeZone).HasMaxLength(100);
        builder.Property(t => t.Currency).HasMaxLength(10);
        builder.Property(t => t.Language).HasMaxLength(10);
        builder.Property(t => t.EmailVerificationToken).HasMaxLength(256);
        builder.Ignore(t => t.HasActiveSubscription);
        builder.Property(t => t.CreatedAt).IsRequired();

        builder.HasMany(t => t.Users)
            .WithOne(u => u.Tenant)
            .HasForeignKey(u => u.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Clients)
            .WithOne(c => c.Tenant)
            .HasForeignKey(c => c.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.ServiceCategories)
            .WithOne(sc => sc.Tenant)
            .HasForeignKey(sc => sc.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.StaffMembers)
            .WithOne(sm => sm.Tenant)
            .HasForeignKey(sm => sm.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Appointments)
            .WithOne(a => a.Tenant)
            .HasForeignKey(a => a.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.WorkingHours)
            .WithOne(wh => wh.Tenant)
            .HasForeignKey(wh => wh.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
