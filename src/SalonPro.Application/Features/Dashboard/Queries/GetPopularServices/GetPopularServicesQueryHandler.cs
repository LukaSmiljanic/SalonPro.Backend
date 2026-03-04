using MediatR;
using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Features.Dashboard.DTOs;
using SalonPro.Application.Features.Dashboard.Queries.GetRevenueChart;
using SalonPro.Domain.Enums;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Dashboard.Queries.GetPopularServices;

public class GetPopularServicesQueryHandler : IRequestHandler<GetPopularServicesQuery, List<PopularServiceDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetPopularServicesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<PopularServiceDto>> Handle(GetPopularServicesQuery request, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        DateTime startDate;

        if (request.Period == ChartPeriod.Week)
        {
            var dayOfWeek = (int)today.DayOfWeek;
            var daysFromMonday = dayOfWeek == 0 ? 6 : dayOfWeek - 1;
            startDate = today.AddDays(-daysFromMonday);
        }
        else
        {
            startDate = new DateTime(today.Year, today.Month, 1);
        }

        var endDate = today.AddDays(1);

        var popularServices = await _unitOfWork.AppointmentServices.Query()
            .Include(aps => aps.Service)
                .ThenInclude(s => s.Category)
            .Include(aps => aps.Appointment)
            .Where(aps =>
                aps.Appointment.StartTime >= startDate &&
                aps.Appointment.StartTime < endDate &&
                aps.Appointment.Status != AppointmentStatus.Cancelled)
            .GroupBy(aps => new
            {
                aps.ServiceId,
                aps.Service.Name,
                aps.Service.Category.Type,
                aps.Service.Category.ColorHex
            })
            .Select(g => new PopularServiceDto(
                g.Key.Name,
                g.Count(),
                g.Key.Type,
                g.Key.ColorHex
            ))
            .OrderByDescending(p => p.BookingCount)
            .Take(10)
            .ToListAsync(cancellationToken);

        return popularServices;
    }
}
