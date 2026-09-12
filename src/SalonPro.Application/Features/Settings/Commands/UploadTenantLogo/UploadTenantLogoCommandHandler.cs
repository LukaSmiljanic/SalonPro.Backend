using MediatR;
using SalonPro.Application.Common.Exceptions;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Application.Features.Settings.DTOs;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Settings.Commands.UploadTenantLogo;

public record UploadTenantLogoCommand(
    string FileName,
    byte[] Content,
    string ContentType) : IRequest<TenantBrandingDto>;

public class UploadTenantLogoCommandHandler : IRequestHandler<UploadTenantLogoCommand, TenantBrandingDto>
{
    private const long MaxBytes = 2 * 1024 * 1024;

    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/jpg", "image/png", "image/webp", "image/svg+xml",
    };

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly IMediaStorageService _mediaStorage;
    private readonly IMediator _mediator;

    public UploadTenantLogoCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentTenantService currentTenantService,
        IMediaStorageService mediaStorage,
        IMediator mediator)
    {
        _unitOfWork = unitOfWork;
        _currentTenantService = currentTenantService;
        _mediaStorage = mediaStorage;
        _mediator = mediator;
    }

    public async Task<TenantBrandingDto> Handle(UploadTenantLogoCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId
            ?? throw new InvalidOperationException("Kontekst salona nije postavljen.");

        var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken)
            ?? throw new InvalidOperationException("Salon nije pronađen.");

        if (request.Content.Length == 0)
            throw new ValidationException("Fajl je prazan.");
        if (request.Content.Length > MaxBytes)
            throw new ValidationException("Maksimalna veličina logotipa je 2 MB.");

        var contentType = request.ContentType?.Trim() ?? "image/png";
        if (!AllowedTypes.Contains(contentType))
            throw new ValidationException("Dozvoljeni formati: JPEG, PNG, WebP, SVG.");

        var extension = contentType switch
        {
            "image/jpeg" or "image/jpg" => ".jpg",
            "image/webp" => ".webp",
            "image/svg+xml" => ".svg",
            _ => ".png",
        };

        if (!string.IsNullOrWhiteSpace(tenant.LogoUrl)
            && !tenant.LogoUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            await _mediaStorage.DeleteByRelativePathAsync(tenant.LogoUrl, cancellationToken);
        }

        tenant.LogoUrl = await _mediaStorage.SaveTenantLogoAsync(
            tenantId, request.Content, extension, cancellationToken);

        _unitOfWork.Tenants.Update(tenant);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new Queries.GetTenantBranding.GetTenantBrandingQuery(), cancellationToken);
    }
}
