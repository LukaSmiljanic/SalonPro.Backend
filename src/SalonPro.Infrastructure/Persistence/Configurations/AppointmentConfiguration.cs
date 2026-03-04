using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalonPro.Domain.Entities;

namespace SalonPro.Infrastructure.Persistence.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.TotalPrice).HasColumnType("decimal(18,2)");
        builder.Property(a => a.Notes).HasMaxLength(2000);
        builder.Property(a => a.CancellationReason).HasMaxLength(1000);
        builder.Property(a => a.CreatedAt).IsRequired();

        builder.HasIndex(a => new { a.TenantId, a.StartTime });
        builder.HasIndex(a => new { a.TenantId, a.StaffMemberId, a.StartTime });

        builder.HasMany(a => a.AppointmentServices)
            .WithOne(aps => aps.Appointment)
            .HasForeignKey(aps => aps.AppointmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
