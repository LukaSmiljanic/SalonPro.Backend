using MediatR;
using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Common.Exceptions;
using SalonPro.Application.Features.Appointments.DTOs;
using SalonPro.Application.Features.Staff.DTOs;
using SalonPro.Domain.Entities;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Staff.Queries.GetStaffSchedule;

public class GetStaffScheduleQueryHandler : IRequestHandler<GetStaffScheduleQuery, StaffScheduleDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetStaffScheduleQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<StaffScheduleDto> Handle(GetStaffScheduleQuery request, CancellationToken cancellationToken)
    {
        var staffMember = await _unitOfWork.StaffMembers.Query()
            .Include(s => s.WorkingHours)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.StaffMemberId, cancellationToken)
            ?? throw new NotFoundException(nameof(StaffMember), request.StaffMemberId);

        var dateStart = request.Date.Date;
        var dateEnd = dateStart.AddDays(1);

        var appointments = await _unitOfWork.Appointments.Query()
            .Include(a => a.Client)
            .Include(a => a.StaffMember)
            .Include(a => a.AppointmentServices)
                .ThenInclude(aps => aps.Service)
                    .ThenInclude(s => s.Category)
            .Where(a =>
                a.StaffMemberId == request.StaffMemberId &&
                a.StartTime >= dateStart &&
                a.StartTime < dateEnd)
            .OrderBy(a => a.StartTime)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var workingHoursDtos = staffMember.WorkingHours
            .OrderBy(wh => wh.DayOfWeek)
            .Select(wh => new WorkingHoursDto(
                wh.Id,
                wh.DayOfWeek,
                wh.StartTime,
                wh.EndTime,
                wh.IsWorkingDay
            )).ToList();

        var appointmentDtos = appointments.Select(a => new AppointmentDto(
            a.Id,
            a.Client.FullName,
            a.StaffMember.FullName,
            string.Join(", ", a.AppointmentServices.Select(aps => aps.Service.Name)),
            a.StartTime,
            a.EndTime,
            a.Status,
            a.TotalPrice,
            a.AppointmentServices.FirstOrDefault()?.Service.Category.ColorHex
        )).ToList();

        return new StaffScheduleDto(
            staffMember.Id,
            staffMember.FullName,
            staffMember.Specialization,
            staffMember.AvatarUrl,
            workingHoursDtos,
            appointmentDtos
        );
    }
}
