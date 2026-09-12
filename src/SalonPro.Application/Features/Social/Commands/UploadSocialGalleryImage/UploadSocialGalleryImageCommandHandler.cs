using MediatR;
using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Common.Exceptions;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Application.Features.Social.DTOs;
using SalonPro.Domain.Entities;
using SalonPro.Domain.Enums;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Social.Commands.UploadSocialGalleryImage;

public record UploadSocialGalleryImageCommand(
    string FileName,
    byte[] Content,
    string ContentType,
    string? CaptionHint) : IRequest<SocialGalleryImageDto>;

public class UploadSocialGalleryImageCommandHandler
    : IRequestHandler<UploadSocialGalleryImageCommand, SocialGalleryImageDto>
{
    private const long MaxBytes = 8 * 1024 * 1024;

    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/jpg", "image/png", "image/webp",
    };

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly IMediaStorageService _mediaStorage;

    public UploadSocialGalleryImageCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentTenantService currentTenantService,
        IMediaStorageService mediaStorage)
    {
        _unitOfWork = unitOfWork;
        _currentTenantService = currentTenantService;
        _mediaStorage = mediaStorage;
    }

    public async Task<SocialGalleryImageDto> Handle(
        UploadSocialGalleryImageCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId
            ?? throw new InvalidOperationException("Kontekst salona nije postavljen.");

        var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken);
        SocialPlanGuard.EnsureAllowed(tenant);

        if (request.Content.Length == 0)
            throw new ValidationException("Fajl je prazan.");
        if (request.Content.Length > MaxBytes)
            throw new ValidationException("Maksimalna veličina slike je 8 MB.");

        var contentType = request.ContentType?.Trim() ?? "image/jpeg";
        if (!AllowedTypes.Contains(contentType))
            throw new ValidationException("Dozvoljeni formati: JPEG, PNG, WebP.");

        var extension = contentType switch
        {
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => ".jpg",
        };

        var imageId = Guid.NewGuid();
        var relativePath = await _mediaStorage.SaveGalleryImageAsync(
            tenantId, imageId, request.Content, extension, cancellationToken);

        var entity = new SocialGalleryImage
        {
            Id = imageId,
            TenantId = tenantId,
            FileName = string.IsNullOrWhiteSpace(request.FileName) ? $"gallery{extension}" : request.FileName.Trim(),
            RelativePath = relativePath,
            ContentType = contentType,
            FileSizeBytes = request.Content.Length,
            CaptionHint = string.IsNullOrWhiteSpace(request.CaptionHint) ? null : request.CaptionHint.Trim(),
        };

        await _unitOfWork.SocialGalleryImages.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new SocialGalleryImageDto(
            entity.Id,
            entity.FileName,
            _mediaStorage.ToRelativeMediaPath(entity.RelativePath),
            entity.CaptionHint,
            entity.FileSizeBytes,
            entity.CreatedAt);
    }
}
