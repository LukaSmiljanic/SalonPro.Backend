using MediatR;
using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Common.Exceptions;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Application.Features.Social.DTOs;
using SalonPro.Domain.Entities;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Social.Queries.GetSocialPosts;

public record GetSocialPostsQuery(DateTime? FromUtc, DateTime? ToUtc) : IRequest<SocialPostsWeekDto>;

public class GetSocialPostsQueryHandler : IRequestHandler<GetSocialPostsQuery, SocialPostsWeekDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IInstagramService _instagramService;
    private readonly IOpenAiService _openAiService;
    private readonly IMediaStorageService _mediaStorage;

    public GetSocialPostsQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentTenantService currentTenantService,
        IDateTimeService dateTimeService,
        IInstagramService instagramService,
        IOpenAiService openAiService,
        IMediaStorageService mediaStorage)
    {
        _unitOfWork = unitOfWork;
        _currentTenantService = currentTenantService;
        _dateTimeService = dateTimeService;
        _instagramService = instagramService;
        _openAiService = openAiService;
        _mediaStorage = mediaStorage;
    }

    public async Task<SocialPostsWeekDto> Handle(GetSocialPostsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId
            ?? throw new InvalidOperationException("Kontekst salona nije postavljen.");

        var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken);
        SocialPlanGuard.EnsureAllowed(tenant);

        var from = request.FromUtc ?? _dateTimeService.UtcNow.Date;
        var to = request.ToUtc ?? from.AddDays(7);

        List<SocialPost> posts;
        try
        {
            posts = await _unitOfWork.SocialPosts.Query()
                .AsNoTracking()
                .Where(p => p.TenantId == tenantId && p.ScheduledAt >= from && p.ScheduledAt < to)
                .OrderBy(p => p.ScheduledAt)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new ValidationException($"SocialPosts upit: {GetDbError(ex)}");
        }

        InstagramConnectionStatus igStatus;
        try
        {
            igStatus = await _instagramService.GetConnectionStatusAsync(tenantId, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new ValidationException($"Instagram tabela: {GetDbError(ex)}");
        }

        return new SocialPostsWeekDto(
            posts.Select(p => SocialPostMapper.ToDto(p, _mediaStorage)).ToList(),
            igStatus.IsConnected,
            igStatus.Username,
            _openAiService.IsConfigured);
    }

    private static string GetDbError(Exception ex)
    {
        while (ex.InnerException != null)
            ex = ex.InnerException;
        return ex.Message;
    }
}

internal static class SocialPostMapper
{
    public static SocialPostDto ToDto(SocialPost p, IMediaStorageService mediaStorage) => new(
        p.Id,
        p.ScheduledAt,
        p.Topic,
        p.Caption,
        p.Hashtags,
        p.ImagePrompt,
        mediaStorage.ResolveImageUrl(p.TenantId, p.Id, p.ImageUrl),
        p.Status,
        p.PublishedAt,
        p.FailureReason,
        p.InstagramMediaId);
}
