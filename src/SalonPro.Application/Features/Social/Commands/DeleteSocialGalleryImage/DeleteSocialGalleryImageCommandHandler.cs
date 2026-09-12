using MediatR;
using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Common.Exceptions;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Social.Commands.DeleteSocialGalleryImage;

public record DeleteSocialGalleryImageCommand(Guid ImageId) : IRequest;

public class DeleteSocialGalleryImageCommandHandler : IRequestHandler<DeleteSocialGalleryImageCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly IMediaStorageService _mediaStorage;

    public DeleteSocialGalleryImageCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentTenantService currentTenantService,
        IMediaStorageService mediaStorage)
    {
        _unitOfWork = unitOfWork;
        _currentTenantService = currentTenantService;
        _mediaStorage = mediaStorage;
    }

    public async Task Handle(DeleteSocialGalleryImageCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId
            ?? throw new InvalidOperationException("Kontekst salona nije postavljen.");

        var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken);
        SocialPlanGuard.EnsureAllowed(tenant);

        var image = await _unitOfWork.SocialGalleryImages.Query()
            .FirstOrDefaultAsync(g => g.Id == request.ImageId && g.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException("Slika nije pronađena.");

        await _mediaStorage.DeleteByRelativePathAsync(image.RelativePath, cancellationToken);
        _unitOfWork.SocialGalleryImages.Delete(image);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
