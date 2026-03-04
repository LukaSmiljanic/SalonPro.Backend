using MediatR;
using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Features.Appointments.DTOs;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Appointments.Queries.GetAppointmentsByDate;

public class GetAppointmentsByDateQueryHandler : IRequestHandler<GetAppointmentsByDateQuery, List<AppointmentDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAppointmentsByDateQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<AppointmentDto>> Handle(GetAppointmentsByDateQuery request, CancellationToken cancellationToken)
    {
        var startOfDay = request.Date.Date;
        var endOfDay = startOfDay.AddDays(1);

        var query = _unitOfWork.Appointments.Query()
            .Include(a => a.Client)
            .Include(a => a.StaffMember)
            .Include(a => a.AppointmentServices)
                .ThenInclude(aps => aps.Service)
            .Where(a => a.StartTime >= startOfDay && a.StartTime < endOfDay)
            .AsNoTracking();

        if (request.StaffMemberId.HasValue)
        {
            query = query.Where(a => a.StaffMemberId == request.StaffMemberId.Value);
        }

        var appointments = await query
            .OrderBy(a => a.StartTime)
            .ToListAsync(cancellationToken);

        return appointments.Select(a => new AppointmentDto(
            a.Id,
            a.Client.FullName,
            a.StaffMember.FullName,
            a.StartTime,
            a.EndTime,
            a.Status,
            a.TotalPrice,
            a.Notes,
            a.AppointmentServices.Select(aps => new AppointmentServiceDto(
                aps.ServiceId,
                aps.Service.Name,
                aps.Price,
                aps.DurationMinutes
            )).ToList()
        )).ToList();
    }
}
