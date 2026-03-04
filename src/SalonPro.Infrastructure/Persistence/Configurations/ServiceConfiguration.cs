using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalonPro.Domain.Entities;

namespace SalonPro.Infrastructure.Persistence.Configurations;

public class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Description).HasMaxLength(1000);
        builder.Property(s => s.Price).HasColumnType("decimal(18,2)");
        builder.Property(s => s.CreatedAt).IsRequired();

        builder.HasMany(s => s.AppointmentServices)
            .WithOne(aps => aps.Service)
            .HasForeignKey(aps => aps.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
