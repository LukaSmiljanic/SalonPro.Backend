using MediatR;
using Microsoft.EntityFrameworkCore;
using SalonPro.Domain.Entities;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Settings.Commands.UpdateWorkingHours;

public class UpdateWorkingHoursCommandHandler : IRequestHandler<UpdateWorkingHoursCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentTenantService _currentTenantService;

    public UpdateWorkingHoursCommandHandler(IUnitOfWork unitOfWork, ICurrentTenantService currentTenantService)
    {
        _unitOfWork = unitOfWork;
        _currentTenantService = currentTenantService;
    }

    public async Task<Unit> Handle(UpdateWorkingHoursCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId
            ?? throw new InvalidOperationException("Tenant ID je obavezan za ažuriranje radnog vremena.");

        // Delete existing salon-level working hours for this tenant
        var existing = await _unitOfWork.WorkingHours.Query()
            .Where(wh => wh.TenantId == tenantId && wh.StaffMemberId == null)
            .ToListAsync(cancellationToken);

        foreach (var item in existing)
            _unitOfWork.WorkingHours.Remove(item);

        // Insert new entries
        foreach (var item in request.Items)
        {
            var workingHours = new WorkingHours
            {
                TenantId = tenantId,
                StaffMemberId = null,
                DayOfWeek = item.DayOfWeek,
                StartTime = item.StartTime,
                EndTime = item.EndTime,
                IsWorkingDay = item.IsWorkingDay
            };
            await _unitOfWork.WorkingHours.AddAsync(workingHours, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
