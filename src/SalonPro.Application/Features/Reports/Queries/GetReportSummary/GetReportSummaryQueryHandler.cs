using MediatR;
using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Features.Reports.DTOs;
using SalonPro.Domain.Enums;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Reports.Queries.GetReportSummary;

public class GetReportSummaryQueryHandler : IRequestHandler<GetReportSummaryQuery, ReportSummaryDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentTenantService _currentTenantService;

    public GetReportSummaryQueryHandler(IUnitOfWork unitOfWork, ICurrentTenantService currentTenantService)
    {
        _unitOfWork = unitOfWork;
        _currentTenantService = currentTenantService;
    }

    public async Task<ReportSummaryDto> Handle(GetReportSummaryQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId
            ?? throw new InvalidOperationException("Tenant ID je obavezan za izveštaj.");

        var appointmentsBase = _unitOfWork.Appointments.Query()
            .Where(a =>
                a.TenantId == tenantId &&
                a.StartTime >= request.From &&
                a.StartTime <= request.To);

        var totalAppointments = await appointmentsBase.CountAsync(cancellationToken);
        var completedCount = await appointmentsBase.CountAsync(a => a.Status == AppointmentStatus.Completed, cancellationToken);
        var cancelledCount = await appointmentsBase.CountAsync(a => a.Status == AppointmentStatus.Cancelled, cancellationToken);
        var noShowCount = await appointmentsBase.CountAsync(a => a.Status == AppointmentStatus.NoShow, cancellationToken);

        var totalRevenue = await appointmentsBase
            .Where(a => a.Status == AppointmentStatus.Completed)
            .SumAsync(a => (decimal?)a.TotalPrice, cancellationToken) ?? 0m;

        var uniqueClients = await appointmentsBase
            .Select(a => a.ClientId)
            .Distinct()
            .CountAsync(cancellationToken);

        // Calculate rates based on total appointments
        var cancellationRate = totalAppointments > 0
            ? Math.Round((decimal)cancelledCount / totalAppointments * 100, 2)
            : 0m;

        var noShowRate = totalAppointments > 0
            ? Math.Round((decimal)noShowCount / totalAppointments * 100, 2)
            : 0m;

        // Average revenue per day in the selected range
        var totalDays = Math.Max(1, (request.To - request.From).TotalDays);
        var averageRevenuePerDay = Math.Round(totalRevenue / (decimal)totalDays, 2);

        return new ReportSummaryDto(
            totalRevenue,
            totalAppointments,
            completedCount,
            cancelledCount,
            noShowCount,
            cancellationRate,
            noShowRate,
            uniqueClients,
            averageRevenuePerDay
        );
    }
}
