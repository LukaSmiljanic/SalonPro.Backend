using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SalonPro.Application.Common;
using SalonPro.Application.Common.Exceptions;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Infrastructure.Social;

/// <summary>
/// Uses OpenAI when configured; otherwise falls back to template generator.
/// </summary>
public class OpenAiSocialContentGenerator : ISocialContentGenerator
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOpenAiService _openAiService;
    private readonly SalonSocialContentGenerator _fallback;
    private readonly ILogger<OpenAiSocialContentGenerator> _logger;

    private static readonly string[] DefaultPostHours = ["10:00", "12:30", "17:00", "11:00", "16:00", "10:30", "18:00"];

    public OpenAiSocialContentGenerator(
        IUnitOfWork unitOfWork,
        IOpenAiService openAiService,
        SalonSocialContentGenerator fallback,
        ILogger<OpenAiSocialContentGenerator> logger)
    {
        _unitOfWork = unitOfWork;
        _openAiService = openAiService;
        _fallback = fallback;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SocialPostDraft>> GenerateWeekAsync(
        Guid tenantId,
        DateTime weekStartLocalDate,
        CancellationToken cancellationToken = default)
    {
        if (!_openAiService.IsConfigured)
        {
            _logger.LogInformation("OpenAI not configured — using template posts.");
            return await _fallback.GenerateWeekAsync(tenantId, weekStartLocalDate, cancellationToken);
        }

        var tenant = await _unitOfWork.Tenants.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken)
            ?? throw new InvalidOperationException("Salon nije pronađen.");

        var services = await _unitOfWork.Services.Query()
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.IsActive)
            .OrderBy(s => s.Name)
            .Select(s => s.Name)
            .Take(12)
            .ToListAsync(cancellationToken);

        var city = string.IsNullOrWhiteSpace(tenant.City) ? "Srbija" : tenant.City.Trim();
        var salon = tenant.Name.Trim();
        var serviceList = services.Count > 0 ? string.Join(", ", services) : "šišanje, feniranje, tretmani";
        var tz = SalonTimeZoneHelper.Resolve(tenant.TimeZone);

        var systemPrompt = """
            Ti si marketing asistent za frizerske i beauty salone u Srbiji.
            Pišeš na srpskom (latinica), prirodan ton, emoji umereno.
            Odgovori ISKLJUČIVO kao JSON objekat sa ključem "posts" — niz od tačno 7 objekata.
            Svaki objekat: topic (kratak naslov), caption (do 900 znakova), hashtags (do 15 hashtagova), imagePrompt (engleski, za DALL-E, bez teksta na slici).
            """;

        var userPrompt = $"""
            Salon: {salon}
            Grad: {city}
            Usluge: {serviceList}
            Nedelja počinje: {weekStartLocalDate:yyyy-MM-dd}

            Napravi 7 različitih Instagram objava (ponedeljak–nedelja): dobrodošlica, savet, ponuda, transformacija, vikend, zahvalnica klijentima, planiranje termina.
            Uključi poziv na DM ili zakazivanje gde ima smisla.
            """;

        var json = await _openAiService.GenerateChatJsonAsync(systemPrompt, userPrompt, cancellationToken);
        if (string.IsNullOrWhiteSpace(json))
        {
            if (_openAiService.IsConfigured && !string.IsNullOrWhiteSpace(_openAiService.LastError))
                throw new ValidationException(_openAiService.LastError);

            _logger.LogWarning("OpenAI returned empty — fallback to templates.");
            return await _fallback.GenerateWeekAsync(tenantId, weekStartLocalDate, cancellationToken);
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var posts = doc.RootElement.GetProperty("posts");
            var drafts = new List<SocialPostDraft>();

            var i = 0;
            foreach (var item in posts.EnumerateArray())
            {
                if (i >= 7) break;

                var day = weekStartLocalDate.Date.AddDays(i);
                var localTime = TimeSpan.Parse(DefaultPostHours[i]);
                var localDt = day.Add(localTime);
                var utc = TimeZoneInfo.ConvertTimeToUtc(
                    DateTime.SpecifyKind(localDt, DateTimeKind.Unspecified), tz);

                drafts.Add(new SocialPostDraft(
                    item.GetProperty("topic").GetString() ?? $"Dan {i + 1}",
                    item.GetProperty("caption").GetString() ?? string.Empty,
                    item.GetProperty("hashtags").GetString() ?? string.Empty,
                    item.GetProperty("imagePrompt").GetString() ?? $"Professional beauty salon in {city}",
                    utc));

                i++;
            }

            if (drafts.Count == 7)
                return drafts;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse OpenAI JSON — fallback.");
        }

        return await _fallback.GenerateWeekAsync(tenantId, weekStartLocalDate, cancellationToken);
    }

    public async Task<SocialPostDraft> GenerateFromGalleryAsync(
        Guid tenantId,
        string? captionHint,
        byte[]? imageBytes = null,
        string? imageContentType = null,
        CancellationToken cancellationToken = default)
    {
        if (!_openAiService.IsConfigured)
            return await _fallback.GenerateFromGalleryAsync(tenantId, captionHint, imageBytes, imageContentType, cancellationToken);

        var tenant = await _unitOfWork.Tenants.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken)
            ?? throw new InvalidOperationException("Salon nije pronađen.");

        var services = await _unitOfWork.Services.Query()
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.IsActive)
            .OrderBy(s => s.Name)
            .Select(s => s.Name)
            .Take(8)
            .ToListAsync(cancellationToken);

        var city = string.IsNullOrWhiteSpace(tenant.City) ? "Srbija" : tenant.City.Trim();
        var salon = tenant.Name.Trim();
        var serviceList = services.Count > 0 ? string.Join(", ", services) : "šišanje, feniranje, tretmani";
        var tz = SalonTimeZoneHelper.Resolve(tenant.TimeZone);
        var hint = string.IsNullOrWhiteSpace(captionHint)
            ? "Salon je podelio autentičnu fotografiju svog rada ili prostora."
            : captionHint.Trim();

        var systemPrompt = """
            Ti si marketing asistent za frizerske i beauty salone u Srbiji.
            Pišeš na srpskom (latinica), prirodan ton, emoji umereno.
            Odgovori ISKLJUČIVO kao JSON objekat sa ključevima: topic, caption (do 900 znakova), hashtags (do 15 hashtagova).
            Ne predlaži generisanje slike — fotografija već postoji.
            """;

        var userPrompt = $"""
            Salon: {salon}
            Grad: {city}
            Usluge: {serviceList}
            Kontekst fotografije: {hint}

            Napiši Instagram objavu koja prati priloženu fotografiju iz galerije salona.
            Opis mora odgovarati onome što je stvarno na slici (proizvod, frizura, enterijer, brend…).
            Uključi poziv na DM ili zakazivanje gde ima smisla.
            """;

        string? json;
        if (imageBytes is { Length: > 0 })
        {
            json = await _openAiService.GenerateChatJsonWithImageAsync(
                systemPrompt,
                userPrompt,
                imageBytes,
                imageContentType ?? "image/jpeg",
                cancellationToken);
        }
        else
        {
            json = await _openAiService.GenerateChatJsonAsync(systemPrompt, userPrompt, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(json))
            return await _fallback.GenerateFromGalleryAsync(tenantId, captionHint, imageBytes, imageContentType, cancellationToken);

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var tomorrow = DateTime.UtcNow.Date.AddDays(1).AddHours(10);
            var utc = TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(tomorrow, DateTimeKind.Unspecified), tz);

            return new SocialPostDraft(
                root.GetProperty("topic").GetString() ?? "Galerija",
                root.GetProperty("caption").GetString() ?? string.Empty,
                root.GetProperty("hashtags").GetString() ?? string.Empty,
                string.Empty,
                utc);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse gallery caption JSON — fallback.");
            return await _fallback.GenerateFromGalleryAsync(tenantId, captionHint, imageBytes, imageContentType, cancellationToken);
        }
    }
}
