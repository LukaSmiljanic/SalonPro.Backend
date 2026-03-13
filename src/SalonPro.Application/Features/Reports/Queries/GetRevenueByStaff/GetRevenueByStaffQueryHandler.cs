using MediatR;
using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Features.Reports.DTOs;
using SalonPro.Domain.Enums;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Reports.Queries.GetRevenueByStaff;

public class GetRevenueByStaffQueryHandler : IRequestHandler<GetRevenueByStaffQuery, List<StaffRevenueDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentTenantService _currentTenantService;

    public GetRevenueByStaffQueryHandler(IUnitOfWork unitOfWork, ICurrentTenantService currentTenantService)
    {
        _unitOfWork = unitOfWork;
        _currentTenantService = currentTenantService;
    }

    public async Task<List<StaffRevenueDto>> Handle(GetRevenueByStaffQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId
            ?? throw new InvalidOperationException("Tenant ID je obavezan za izveštaj po zaposlenima.");

        var results = await _unitOfWork.Appointments.Query()
            .Where(a =>
                a.TenantId == tenantId &&
                a.Status == AppointmentStatus.Completed &&
                a.StartTime >= request.From &&
                a.StartTime <= request.To)
            .GroupBy(a => new { a.StaffMemberId, a.StaffMember.FirstName, a.StaffMember.LastName })
            .Select(g => new
            {
                StaffId = g.Key.StaffMemberId.ToString(),
                StaffName = g.Key.FirstName + " " + g.Key.LastName,
                TotalRevenue = g.Sum(a => a.TotalPrice),
                AppointmentCount = g.Count()
            })
            .OrderByDescending(r => r.TotalRevenue)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return results.Select(r => new StaffRevenueDto(
            r.StaffId,
            r.StaffName.Trim(),
            r.TotalRevenue,
            r.AppointmentCount,
            r.AppointmentCount > 0 ? Math.Round(r.TotalRevenue / r.AppointmentCount, 2) : 0m
        )).ToList();
    }
}
