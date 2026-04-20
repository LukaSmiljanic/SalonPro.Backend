using MediatR;
using SalonPro.Application.Common;
using SalonPro.Application.Common.Exceptions;
using SalonPro.Domain.Entities;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Tenants.Commands.UpdateTenantPlan;

public class UpdateTenantPlanCommandHandler : IRequestHandler<UpdateTenantPlanCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateTenantPlanCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(UpdateTenantPlanCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _unitOfWork.Tenants.GetByIdAsync(request.TenantId, cancellationToken)
            ?? throw new NotFoundException(nameof(Tenant), request.TenantId);

        tenant.Plan = TenantPlanRules.Normalize(request.Plan);

        _unitOfWork.Tenants.Update(tenant);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

