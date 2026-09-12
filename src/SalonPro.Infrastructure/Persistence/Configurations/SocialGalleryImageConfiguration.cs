using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalonPro.Domain.Entities;

namespace SalonPro.Infrastructure.Persistence.Configurations;

public class SocialGalleryImageConfiguration : IEntityTypeConfiguration<SocialGalleryImage>
{
    public void Configure(EntityTypeBuilder<SocialGalleryImage> builder)
    {
        builder.HasKey(g => g.Id);
        builder.Property(g => g.FileName).IsRequired().HasMaxLength(260);
        builder.Property(g => g.RelativePath).IsRequired().HasMaxLength(500);
        builder.Property(g => g.ContentType).IsRequired().HasMaxLength(100);
        builder.Property(g => g.CaptionHint).HasMaxLength(500);
        builder.Property(g => g.CreatedAt).IsRequired();
        builder.HasIndex(g => new { g.TenantId, g.CreatedAt });

        builder.HasOne(g => g.Tenant)
            .WithMany()
            .HasForeignKey(g => g.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
