using MediatR;
using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Common;
using SalonPro.Application.Features.Appointments.DTOs;
using SalonPro.Domain.Enums;
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

        if (appointments.Count == 0)
            return new List<AppointmentDto>();

        var tenantId = appointments[0].TenantId;
        var thresholds = await AppointmentLoyaltyProjection.LoadMilestoneThresholdsAsync(
            tenantId, _unitOfWork, cancellationToken);

        var clientIds = appointments.Select(a => a.ClientId).Distinct().ToList();
        var completedRows = await _unitOfWork.Appointments.Query()
            .Where(a => clientIds.Contains(a.ClientId) && a.Status == AppointmentStatus.Completed)
            .Select(a => new { a.Id, a.ClientId, a.StartTime })
            .ToListAsync(cancellationToken);

        var completedIndex = AppointmentLoyaltyProjection.IndexCompletedVisits(
            completedRows.Select(r => (r.Id, r.ClientId, r.StartTime)).ToList());

        return appointments.Select(a =>
        {
            var (vn, mile) = AppointmentLoyaltyProjection.ComputeVisitInfo(
                a.Id, a.ClientId, a.StartTime, completedIndex, thresholds);
            return AppointmentLoyaltyProjection.ToAppointmentDto(a, vn, mile);
        }).ToList();
    }
}
