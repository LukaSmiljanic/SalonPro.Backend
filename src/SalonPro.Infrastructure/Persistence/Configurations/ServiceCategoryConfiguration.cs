using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalonPro.Domain.Entities;

namespace SalonPro.Infrastructure.Persistence.Configurations;

public class ServiceCategoryConfiguration : IEntityTypeConfiguration<ServiceCategory>
{
    public void Configure(EntityTypeBuilder<ServiceCategory> builder)
    {
        builder.HasKey(sc => sc.Id);
        builder.Property(sc => sc.Name).IsRequired().HasMaxLength(200);
        builder.Property(sc => sc.Description).HasMaxLength(1000);
        builder.Property(sc => sc.Color).HasMaxLength(20);
        builder.Property(sc => sc.ColorHex).HasMaxLength(20);
        builder.Property(sc => sc.CreatedAt).IsRequired();

        builder.HasMany(sc => sc.Services)
            .WithOne(s => s.Category)
            .HasForeignKey(s => s.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
