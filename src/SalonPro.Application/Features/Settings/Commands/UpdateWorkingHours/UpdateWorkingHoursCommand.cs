using MediatR;

namespace SalonPro.Application.Features.Settings.Commands.UpdateWorkingHours;

public record WorkingHourItem(
    DayOfWeek DayOfWeek,
    TimeSpan StartTime,
    TimeSpan EndTime,
    bool IsWorkingDay
);

public record UpdateWorkingHoursCommand(List<WorkingHourItem> Items) : IRequest<Unit>;
