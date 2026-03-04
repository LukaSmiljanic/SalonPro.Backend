using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalonPro.Domain.Entities;

namespace SalonPro.Infrastructure.Persistence.Configurations;

public class StaffMemberConfiguration : IEntityTypeConfiguration<StaffMember>
{
    public void Configure(EntityTypeBuilder<StaffMember> builder)
    {
        builder.HasKey(sm => sm.Id);
        builder.Property(sm => sm.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(sm => sm.LastName).IsRequired().HasMaxLength(100);
        builder.Property(sm => sm.Email).HasMaxLength(256);
        builder.Property(sm => sm.Phone).HasMaxLength(20);
        builder.Property(sm => sm.Title).HasMaxLength(200);
        builder.Property(sm => sm.Specialization).HasMaxLength(200);
        builder.Property(sm => sm.AvatarUrl).HasMaxLength(2000);
        builder.Property(sm => sm.CreatedAt).IsRequired();

        builder.HasMany(sm => sm.Appointments)
            .WithOne(a => a.StaffMember)
            .HasForeignKey(a => a.StaffMemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(sm => sm.WorkingHours)
            .WithOne(wh => wh.StaffMember)
            .HasForeignKey(wh => wh.StaffMemberId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sm => sm.User)
            .WithMany()
            .HasForeignKey(sm => sm.UserId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
    }
}
