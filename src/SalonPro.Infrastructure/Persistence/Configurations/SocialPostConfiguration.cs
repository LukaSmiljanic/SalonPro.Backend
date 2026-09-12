using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalonPro.Domain.Entities;

namespace SalonPro.Infrastructure.Persistence.Configurations;

public class SocialPostConfiguration : IEntityTypeConfiguration<SocialPost>
{
    public void Configure(EntityTypeBuilder<SocialPost> builder)
    {
        builder.ToTable("SocialPosts");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Topic).IsRequired().HasMaxLength(120);
        builder.Property(p => p.Caption).IsRequired().HasMaxLength(2200);
        builder.Property(p => p.Hashtags).HasMaxLength(500);
        builder.Property(p => p.ImagePrompt).HasMaxLength(500);
        builder.Property(p => p.ImageUrl).HasMaxLength(1000);
        builder.Property(p => p.InstagramMediaId).HasMaxLength(128);
        builder.Property(p => p.FailureReason).HasMaxLength(500);
        builder.Ignore(p => p.LastModifiedAt);
        builder.HasIndex(p => new { p.TenantId, p.ScheduledAt });
        builder.HasOne(p => p.Tenant).WithMany().HasForeignKey(p => p.TenantId).OnDelete(DeleteBehavior.Cascade);
    }
}
