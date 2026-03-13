using MediatR;
using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Features.Reports.DTOs;
using SalonPro.Domain.Enums;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Reports.Queries.GetRevenueByService;

public class GetRevenueByServiceQueryHandler : IRequestHandler<GetRevenueByServiceQuery, List<ServiceRevenueDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentTenantService _currentTenantService;

    public GetRevenueByServiceQueryHandler(IUnitOfWork unitOfWork, ICurrentTenantService currentTenantService)
    {
        _unitOfWork = unitOfWork;
        _currentTenantService = currentTenantService;
    }

    public async Task<List<ServiceRevenueDto>> Handle(GetRevenueByServiceQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId
            ?? throw new InvalidOperationException("Tenant ID je obavezan za izveštaj po uslugama.");

        var results = await _unitOfWork.AppointmentServices.Query()
            .Where(aps =>
                aps.Appointment.TenantId == tenantId &&
                aps.Appointment.Status == AppointmentStatus.Completed &&
                aps.Appointment.StartTime >= request.From &&
                aps.Appointment.StartTime <= request.To)
            .GroupBy(aps => new { aps.Service.Name, CategoryName = aps.Service.Category.Name })
            .Select(g => new
            {
                ServiceName = g.Key.Name,
                Category = g.Key.CategoryName,
                TotalRevenue = g.Sum(aps => aps.Price),
                BookingCount = g.Count()
            })
            .OrderByDescending(r => r.TotalRevenue)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return results.Select(r => new ServiceRevenueDto(
            r.ServiceName,
            r.Category,
            r.TotalRevenue,
            r.BookingCount,
            r.BookingCount > 0 ? Math.Round(r.TotalRevenue / r.BookingCount, 2) : 0m
        )).ToList();
    }
}
