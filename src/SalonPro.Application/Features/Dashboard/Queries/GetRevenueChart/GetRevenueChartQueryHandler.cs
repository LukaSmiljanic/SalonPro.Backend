using MediatR;
using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Features.Dashboard.DTOs;
using SalonPro.Domain.Enums;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Dashboard.Queries.GetRevenueChart;

public class GetRevenueChartQueryHandler : IRequestHandler<GetRevenueChartQuery, RevenueChartDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentTenantService _currentTenantService;

    private static readonly string[] SerbianDayAbbreviations = { "Ned", "Pon", "Uto", "Sre", "Čet", "Pet", "Sub" };
    private static readonly string[] SerbianMonthAbbreviations = { "Jan", "Feb", "Mar", "Apr", "Maj", "Jun", "Jul", "Avg", "Sep", "Okt", "Nov", "Dec" };

    public GetRevenueChartQueryHandler(IUnitOfWork unitOfWork, ICurrentTenantService currentTenantService)
    {
        _unitOfWork = unitOfWork;
        _currentTenantService = currentTenantService;
    }

    public async Task<RevenueChartDto> Handle(GetRevenueChartQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId
            ?? throw new InvalidOperationException("Kontekst salona nije postavljen.");

        var today = DateTime.UtcNow.Date;
        DateTime startDate;
        DateTime endDate = today.AddDays(1);

        if (request.Days.HasValue && request.Days.Value > 0)
        {
            startDate = today.AddDays(-(request.Days.Value - 1));
        }
        else if (request.Period == ChartPeriod.Week)
        {
            var dayOfWeek = (int)today.DayOfWeek;
            var daysFromMonday = dayOfWeek == 0 ? 6 : dayOfWeek - 1;
            startDate = today.AddDays(-daysFromMonday);
        }
        else
        {
            startDate = new DateTime(today.Year, today.Month, 1);
        }

        var appointments = await _unitOfWork.Appointments.Query()
            .Where(a => a.TenantId == tenantId &&
                a.StartTime >= startDate &&
                a.StartTime < endDate &&
                a.Status == AppointmentStatus.Completed)
            .Select(a => new { a.StartTime, a.TotalPrice })
            .ToListAsync(cancellationToken);

        var dataPoints = new List<RevenueDataPoint>();
        var dayCount = request.Days.HasValue && request.Days.Value > 0
            ? request.Days.Value
            : (request.Period == ChartPeriod.Week ? 7 : DateTime.DaysInMonth(today.Year, today.Month));

        for (int i = 0; i < dayCount; i++)
        {
            var day = startDate.AddDays(i);
            if (day > today && !request.Days.HasValue) break;

            var dayRevenue = appointments
                .Where(a => a.StartTime.Date == day)
                .Sum(a => a.TotalPrice);

            var label = request.Days.HasValue && request.Days.Value > 0
                ? $"{day.Day} {SerbianMonthAbbreviations[day.Month - 1]}"
                : request.Period == ChartPeriod.Week
                    ? SerbianDayAbbreviations[(int)day.DayOfWeek]
                    : $"{day.Day} {SerbianMonthAbbreviations[day.Month - 1]}";

            dataPoints.Add(new RevenueDataPoint(label, dayRevenue, day));
        }

        return new RevenueChartDto(dataPoints);
    }
}
