using SalonPro.Domain.Common;

namespace SalonPro.Domain.Entities;

public class AppointmentService : BaseEntity
{
    public Guid AppointmentId { get; set; }
    public Guid ServiceId { get; set; }
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; }

    // Navigation
    public Appointment Appointment { get; set; } = null!;
    public Service Service { get; set; } = null!;
}
