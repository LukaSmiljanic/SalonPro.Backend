using SalonPro.Application.Common.Interfaces;

namespace SalonPro.Infrastructure.Services;

public class DateTimeService : IDateTimeService
{
    public DateTime UtcNow => DateTime.UtcNow;
}
