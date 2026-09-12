using MediatR;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Application.Features.Social;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Social.Queries.GetSocialDbDiagnostics;

public record GetSocialDbDiagnosticsQuery : IRequest<SocialDbDiagnosticsDto>;

public class GetSocialDbDiagnosticsQueryHandler : IRequestHandler<GetSocialDbDiagnosticsQuery, SocialDbDiagnosticsDto>
{
    private readonly ISocialDatabaseDiagnostics _diagnostics;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentTenantService _currentTenantService;

    public GetSocialDbDiagnosticsQueryHandler(
        ISocialDatabaseDiagnostics diagnostics,
        IUnitOfWork unitOfWork,
        ICurrentTenantService currentTenantService)
    {
        _diagnostics = diagnostics;
        _unitOfWork = unitOfWork;
        _currentTenantService = currentTenantService;
    }

    public async Task<SocialDbDiagnosticsDto> Handle(
        GetSocialDbDiagnosticsQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId
            ?? throw new InvalidOperationException("Kontekst salona nije postavljen.");

        var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken);
        SocialPlanGuard.EnsureAllowed(tenant);

        return await _diagnostics.InspectAsync(cancellationToken);
    }
}
