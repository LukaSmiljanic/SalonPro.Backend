using MediatR;
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
            ?? throw new InvalidOperationException("Tenant context is not set.");

        var staffMember = new StaffMember
        {
            TenantId = tenantId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            Specialization = request.Specialization,
            IsActive = true
        };

        await _unitOfWork.StaffMembers.AddAsync(staffMember, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return staffMember.Id;
    }
}
