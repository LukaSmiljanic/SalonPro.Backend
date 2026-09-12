using System.Text.RegularExpressions;
using MediatR;
using SalonPro.Application.Common.Exceptions;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Application.Features.Settings.DTOs;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Settings.Commands.UpdateTenantBranding;

public record UpdateTenantBrandingCommand(
    string? PrimaryColor,
    string? AccentColor,
    bool OnlineBookingEnabled) : IRequest<TenantBrandingDto>;

public class UpdateTenantBrandingCommandHandler : IRequestHandler<UpdateTenantBrandingCommand, TenantBrandingDto>
{
    private static readonly Regex HexColor = new(@"^#([0-9A-Fa-f]{3}|[0-9A-Fa-f]{6})$", RegexOptions.Compiled);

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly IMediator _mediator;

    public UpdateTenantBrandingCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentTenantService currentTenantService,
        IMediator mediator)
    {
        _unitOfWork = unitOfWork;
        _currentTenantService = currentTenantService;
        _mediator = mediator;
    }

    public async Task<TenantBrandingDto> Handle(UpdateTenantBrandingCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId
            ?? throw new InvalidOperationException("Kontekst salona nije postavljen.");

        var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken)
            ?? throw new InvalidOperationException("Salon nije pronađen.");

        if (!string.IsNullOrWhiteSpace(request.PrimaryColor) && !HexColor.IsMatch(request.PrimaryColor.Trim()))
            throw new ValidationException("Primarna boja mora biti u hex formatu (#RRGGBB).");

        if (!string.IsNullOrWhiteSpace(request.AccentColor) && !HexColor.IsMatch(request.AccentColor.Trim()))
            throw new ValidationException("Akcentna boja mora biti u hex formatu (#RRGGBB).");

        tenant.PrimaryColor = string.IsNullOrWhiteSpace(request.PrimaryColor)
            ? tenant.PrimaryColor
            : request.PrimaryColor.Trim();
        tenant.AccentColor = string.IsNullOrWhiteSpace(request.AccentColor)
            ? tenant.AccentColor
            : request.AccentColor.Trim();
        tenant.OnlineBookingEnabled = request.OnlineBookingEnabled;

        _unitOfWork.Tenants.Update(tenant);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new Queries.GetTenantBranding.GetTenantBrandingQuery(), cancellationToken);
    }
}
