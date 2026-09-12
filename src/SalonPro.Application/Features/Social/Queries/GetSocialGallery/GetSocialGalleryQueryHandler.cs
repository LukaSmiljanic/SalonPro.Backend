using MediatR;
using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Application.Features.Social.DTOs;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Social.Queries.GetSocialGallery;

public record GetSocialGalleryQuery : IRequest<IReadOnlyList<SocialGalleryImageDto>>;

public class GetSocialGalleryQueryHandler : IRequestHandler<GetSocialGalleryQuery, IReadOnlyList<SocialGalleryImageDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly IMediaStorageService _mediaStorage;

    public GetSocialGalleryQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentTenantService currentTenantService,
        IMediaStorageService mediaStorage)
    {
        _unitOfWork = unitOfWork;
        _currentTenantService = currentTenantService;
        _mediaStorage = mediaStorage;
    }

    public async Task<IReadOnlyList<SocialGalleryImageDto>> Handle(
        GetSocialGalleryQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId
            ?? throw new InvalidOperationException("Kontekst salona nije postavljen.");

        var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken);
        SocialPlanGuard.EnsureAllowed(tenant);

        var images = await _unitOfWork.SocialGalleryImages.Query()
            .AsNoTracking()
            .Where(g => g.TenantId == tenantId)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync(cancellationToken);

        return images.Select(g => new SocialGalleryImageDto(
            g.Id,
            g.FileName,
            _mediaStorage.ToRelativeMediaPath(g.RelativePath),
            g.CaptionHint,
            g.FileSizeBytes,
            g.CreatedAt)).ToList();
    }
}
