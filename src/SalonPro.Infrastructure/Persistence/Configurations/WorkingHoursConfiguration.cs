using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalonPro.Domain.Entities;

namespace SalonPro.Infrastructure.Persistence.Configurations;

public class WorkingHoursConfiguration : IEntityTypeConfiguration<WorkingHours>
{
    public void Configure(EntityTypeBuilder<WorkingHours> builder)
    {
        builder.HasKey(wh => wh.Id);
        builder.Property(wh => wh.StartTime).IsRequired();
        builder.Property(wh => wh.EndTime).IsRequired();
        builder.HasIndex(wh => new { wh.TenantId, wh.StaffMemberId, wh.DayOfWeek });
    }
}
