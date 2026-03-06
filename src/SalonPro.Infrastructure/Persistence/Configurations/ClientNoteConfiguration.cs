using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalonPro.Domain.Entities;

namespace SalonPro.Infrastructure.Persistence.Configurations;

public class ClientNoteConfiguration : IEntityTypeConfiguration<ClientNote>
{
    public void Configure(EntityTypeBuilder<ClientNote> builder)
    {
        builder.HasKey(cn => cn.Id);
        builder.Property(cn => cn.Content).IsRequired().HasMaxLength(5000);
        builder.Property(cn => cn.CreatedBy).HasMaxLength(256);
        builder.Property(cn => cn.CreatedAt).IsRequired();

        builder.HasOne(cn => cn.Tenant)
            .WithMany()
            .HasForeignKey(cn => cn.TenantId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
