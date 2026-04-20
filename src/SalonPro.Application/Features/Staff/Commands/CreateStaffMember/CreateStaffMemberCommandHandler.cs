using MediatR;
using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Common;
using SalonPro.Application.Common.Exceptions;
using SalonPro.Domain.Entities;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Staff.Commands.CreateStaffMember;

public class CreateStaffMemberCommandHandler : IRequestHandler<CreateStaffMemberCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentTenantService _currentTenantService;

    public CreateStaffMemberCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentTenantService currentTenantService)
    {
        _unitOfWork = unitOfWork;
        _currentTenantService = currentTenantService;
    }

    public async Task<Guid> Handle(CreateStaffMemberCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId
            ?? throw new InvalidOperationException("Kontekst salona nije postavljen.");

        var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken)
            ?? throw new NotFoundException(nameof(Tenant), tenantId);

        var maxStaffMembers = TenantPlanRules.MaxStaffMembers(tenant.Plan);
        if (maxStaffMembers != int.MaxValue)
        {
            var activeStaffCount = await _unitOfWork.StaffMembers.Query()
                .CountAsync(s => s.TenantId == tenantId && s.IsActive, cancellationToken);

            if (activeStaffCount >= maxStaffMembers)
            {
                throw new ValidationException(
                    $"Vaš paket ({TenantPlanRules.Normalize(tenant.Plan)}) dozvoljava najviše {maxStaffMembers} aktivnih zaposlenih. Nadogradite paket za više članova osoblja.");
            }
        }

        var staffMember = new StaffMember
        {
            TenantId = tenantId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            Title = request.Specialization ?? request.Title,
            ColorIndex = request.ColorIndex,
            IsActive = true
        };

        await _unitOfWork.StaffMembers.AddAsync(staffMember, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return staffMember.Id;
    }
}
