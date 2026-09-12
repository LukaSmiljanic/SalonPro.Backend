using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalonPro.Domain.Entities;

namespace SalonPro.Infrastructure.Persistence.Configurations;

public class TenantInstagramAccountConfiguration : IEntityTypeConfiguration<TenantInstagramAccount>
{
    public void Configure(EntityTypeBuilder<TenantInstagramAccount> builder)
    {
        builder.ToTable("TenantInstagramAccounts");
        builder.HasKey(a => a.Id);
        builder.HasIndex(a => a.TenantId).IsUnique();
        builder.Property(a => a.FacebookPageId).IsRequired().HasMaxLength(64);
        builder.Property(a => a.InstagramUserId).IsRequired().HasMaxLength(64);
        builder.Property(a => a.InstagramUsername).HasMaxLength(128);
        builder.Property(a => a.ProtectedAccessToken).IsRequired();
        builder.Ignore(a => a.LastModifiedAt);
        builder.HasOne(a => a.Tenant).WithMany().HasForeignKey(a => a.TenantId).OnDelete(DeleteBehavior.Cascade);
    }
}
