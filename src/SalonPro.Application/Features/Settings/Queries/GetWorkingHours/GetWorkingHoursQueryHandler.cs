using MediatR;
using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Features.Settings.DTOs;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Settings.Queries.GetWorkingHours;

public class GetWorkingHoursQueryHandler : IRequestHandler<GetWorkingHoursQuery, List<WorkingHoursDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentTenantService _currentTenantService;

    public GetWorkingHoursQueryHandler(IUnitOfWork unitOfWork, ICurrentTenantService currentTenantService)
    {
        _unitOfWork = unitOfWork;
        _currentTenantService = currentTenantService;
    }

    public async Task<List<WorkingHoursDto>> Handle(GetWorkingHoursQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId
            ?? throw new InvalidOperationException("Tenant ID je obavezan za dohvatanje radnog vremena.");

        var workingHours = await _unitOfWork.WorkingHours.Query()
            .Where(wh => wh.TenantId == tenantId && wh.StaffMemberId == null)
            .OrderBy(wh => wh.DayOfWeek)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return workingHours
            .Select(wh => new WorkingHoursDto(wh.DayOfWeek, wh.StartTime, wh.EndTime, wh.IsWorkingDay))
            .ToList();
    }
}
