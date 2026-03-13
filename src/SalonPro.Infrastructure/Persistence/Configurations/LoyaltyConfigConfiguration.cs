using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalonPro.Domain.Entities;

namespace SalonPro.Infrastructure.Persistence.Configurations;

public class LoyaltyConfigConfiguration : IEntityTypeConfiguration<LoyaltyConfig>
{
    public void Configure(EntityTypeBuilder<LoyaltyConfig> builder)
    {
        builder.ToTable("LoyaltyConfigs");
        builder.HasKey(lc => lc.Id);
        builder.Property(lc => lc.TierName).IsRequired().HasMaxLength(50);
        builder.Property(lc => lc.Benefit).IsRequired().HasMaxLength(200);
        builder.HasOne(lc => lc.Tenant).WithMany().HasForeignKey(lc => lc.TenantId).OnDelete(DeleteBehavior.Cascade);
    }
}
