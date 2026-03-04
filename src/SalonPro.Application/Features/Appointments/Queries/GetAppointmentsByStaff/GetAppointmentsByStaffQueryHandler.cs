using MediatR;
using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Features.Appointments.DTOs;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Appointments.Queries.GetAppointmentsByStaff;

public class GetAppointmentsByStaffQueryHandler : IRequestHandler<GetAppointmentsByStaffQuery, List<AppointmentDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAppointmentsByStaffQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<AppointmentDto>> Handle(GetAppointmentsByStaffQuery request, CancellationToken cancellationToken)
    {
        var appointments = await _unitOfWork.Appointments.Query()
            .Include(a => a.Client)
            .Include(a => a.StaffMember)
            .Include(a => a.AppointmentServices)
                .ThenInclude(aps => aps.Service)
            .Where(a =>
                a.StaffMemberId == request.StaffMemberId &&
                a.StartTime >= request.StartDate &&
                a.StartTime <= request.EndDate)
            .OrderBy(a => a.StartTime)
            .AsNoTracking()
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
