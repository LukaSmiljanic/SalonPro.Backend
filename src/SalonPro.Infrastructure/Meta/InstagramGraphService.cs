using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Domain.Entities;
using SalonPro.Infrastructure.Persistence;

namespace SalonPro.Infrastructure.Meta;

public class InstagramGraphService : IInstagramService
{
    private readonly ApplicationDbContext _context;
    private readonly ISecretProtector _secretProtector;
    private readonly MetaSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<InstagramGraphService> _logger;

    private static readonly string[] DefaultScopes =
    [
        "instagram_basic",
        "instagram_content_publish",
        "pages_show_list",
        "pages_read_engagement",
    ];

    private string[] ResolveScopes() =>
        _settings.OAuthScopes is { Length: > 0 } ? _settings.OAuthScopes : DefaultScopes;

    public IReadOnlyList<string> GetRequestedScopes() => ResolveScopes();

    public InstagramGraphService(
        ApplicationDbContext context,
        ISecretProtector secretProtector,
        IOptions<MetaSettings> settings,
        IHttpClientFactory httpClientFactory,
        ILogger<InstagramGraphService> logger)
    {
        _context = context;
        _secretProtector = secretProtector;
        _settings = settings.Value;
        _httpClient = httpClientFactory.CreateClient("MetaGraph");
        _logger = logger;
    }

    public bool IsConfigured =>
        _settings.Enabled
        && !string.IsNullOrWhiteSpace(_settings.AppId)
        && !string.IsNullOrWhiteSpace(_settings.AppSecret)
        && !string.IsNullOrWhiteSpace(_settings.RedirectUri);

    public string? AppId => string.IsNullOrWhiteSpace(_settings.AppId) ? null : _settings.AppId;

    public string? RedirectUri => string.IsNullOrWhiteSpace(_settings.RedirectUri) ? null : _settings.RedirectUri;

    public string BuildConnectUrl(string state)
    {
        var qs = string.Join("&", new Dictionary<string, string>
        {
            ["client_id"] = _settings.AppId,
            ["redirect_uri"] = _settings.RedirectUri,
            ["scope"] = string.Join(",", ResolveScopes()),
            ["response_type"] = "code",
            ["state"] = state,
            ["auth_type"] = "rerequest",
        }.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));

        return $"https://www.facebook.com/{_settings.GraphApiVersion}/dialog/oauth?{qs}";
    }

    public async Task<InstagramConnectionStatus> GetConnectionStatusAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var account = await _context.TenantInstagramAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.TenantId == tenantId, cancellationToken);

        if (account == null)
            return new InstagramConnectionStatus(false, null, null);

        return new InstagramConnectionStatus(true, account.InstagramUsername, account.TokenExpiresAt);
    }

    public async Task CompleteOAuthAsync(Guid tenantId, string code, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("Meta aplikacija nije konfigurisana.");

        var shortToken = await ExchangeCodeForTokenAsync(code, cancellationToken);
        var longToken = await ExchangeForLongLivedTokenAsync(shortToken, cancellationToken);

        var resolved = await ResolvePageWithInstagramAsync(longToken, cancellationToken);
        if (resolved == null)
        {
            throw new InvalidOperationException(
                "Meta nije vratio nijednu Facebook stranicu za ovu aplikaciju. " +
                "U Business integrations uklonite salonpro, pa ponovo Poveži Instagram i izaberite SalonPro Page.");
        }

        var (pageId, pageToken, igUserId) = resolved.Value;

        string? username = null;
        try
        {
            var igInfo = await GetJsonAsync(
                $"{_settings.GraphApiVersion}/{igUserId}?fields=username&access_token={pageToken}",
                cancellationToken);
            username = igInfo.TryGetProperty("username", out var u) ? u.GetString() : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not fetch IG username for {IgUserId}", igUserId);
        }

        var existing = await _context.TenantInstagramAccounts
            .FirstOrDefaultAsync(a => a.TenantId == tenantId, cancellationToken);

        if (existing == null)
        {
            existing = new TenantInstagramAccount { TenantId = tenantId };
            await _context.TenantInstagramAccounts.AddAsync(existing, cancellationToken);
        }

        existing.FacebookPageId = pageId;
        existing.InstagramUserId = igUserId;
        existing.InstagramUsername = username;
        existing.ProtectedAccessToken = _secretProtector.Protect(pageToken);
        existing.TokenExpiresAt = DateTime.UtcNow.AddDays(55);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DisconnectAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var account = await _context.TenantInstagramAccounts
            .FirstOrDefaultAsync(a => a.TenantId == tenantId, cancellationToken);

        if (account != null)
        {
            _context.TenantInstagramAccounts.Remove(account);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<string> PublishImagePostAsync(
        Guid tenantId,
        string imageUrl,
        string caption,
        CancellationToken cancellationToken = default)
    {
        var account = await _context.TenantInstagramAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.TenantId == tenantId, cancellationToken)
            ?? throw new InvalidOperationException("Instagram nalog nije povezan.");

        var token = _secretProtector.Unprotect(account.ProtectedAccessToken);
        var igUserId = account.InstagramUserId;

        var createPayload = new Dictionary<string, string>
        {
            ["image_url"] = imageUrl,
            ["caption"] = caption,
            ["access_token"] = token,
        };

        using var createContent = new FormUrlEncodedContent(createPayload);
        using var createResponse = await _httpClient.PostAsync(
            $"{_settings.GraphApiVersion}/{igUserId}/media",
            createContent,
            cancellationToken);

        var createBody = await createResponse.Content.ReadAsStringAsync(cancellationToken);
        if (!createResponse.IsSuccessStatusCode)
            throw new InvalidOperationException($"Instagram media create failed: {createBody}");

        using var createDoc = JsonDocument.Parse(createBody);
        var creationId = createDoc.RootElement.GetProperty("id").GetString()!;

        var publishPayload = new Dictionary<string, string>
        {
            ["creation_id"] = creationId,
            ["access_token"] = token,
        };

        using var publishContent = new FormUrlEncodedContent(publishPayload);
        using var publishResponse = await _httpClient.PostAsync(
            $"{_settings.GraphApiVersion}/{igUserId}/media_publish",
            publishContent,
            cancellationToken);

        var publishBody = await publishResponse.Content.ReadAsStringAsync(cancellationToken);
        if (!publishResponse.IsSuccessStatusCode)
            throw new InvalidOperationException($"Instagram publish failed: {publishBody}");

        using var publishDoc = JsonDocument.Parse(publishBody);
        return publishDoc.RootElement.GetProperty("id").GetString()!;
    }

    private async Task<(string PageId, string PageToken, string IgUserId)?> ResolvePageWithInstagramAsync(
        string userToken,
        CancellationToken cancellationToken)
    {
        var fromAccounts = await TryResolveFromMeAccountsAsync(userToken, cancellationToken);
        if (fromAccounts != null)
            return fromAccounts;

        _logger.LogWarning("Meta /me/accounts returned no pages — trying business portfolio fallback.");
        return await TryResolveFromBusinessPagesAsync(userToken, cancellationToken);
    }

    private async Task<(string PageId, string PageToken, string IgUserId)?> TryResolveFromMeAccountsAsync(
        string userToken,
        CancellationToken cancellationToken)
    {
        try
        {
            var pages = await GetJsonAsync(
                $"{_settings.GraphApiVersion}/me/accounts?fields=id,access_token,instagram_business_account&access_token={userToken}",
                cancellationToken);

            if (!pages.TryGetProperty("data", out var data))
                return null;

            foreach (var page in data.EnumerateArray())
            {
                if (!page.TryGetProperty("instagram_business_account", out var ig)
                    || ig.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var pageId = page.GetProperty("id").GetString()!;
                var pageToken = page.TryGetProperty("access_token", out var tok)
                    ? tok.GetString()
                    : await FetchPageAccessTokenAsync(pageId, userToken, cancellationToken);

                if (pageToken == null || !ig.TryGetProperty("id", out var igId))
                    continue;

                return (pageId, pageToken, igId.GetString()!);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve pages from /me/accounts");
        }

        return null;
    }

    private async Task<(string PageId, string PageToken, string IgUserId)?> TryResolveFromBusinessPagesAsync(
        string userToken,
        CancellationToken cancellationToken)
    {
        try
        {
            var businesses = await GetJsonAsync(
                $"{_settings.GraphApiVersion}/me/businesses?fields=id,name&access_token={userToken}",
                cancellationToken);

            if (!businesses.TryGetProperty("data", out var bizList))
                return null;

            foreach (var biz in bizList.EnumerateArray())
            {
                var bizId = biz.GetProperty("id").GetString()!;
                var resolved = await TryResolveFromBusinessPageListAsync(
                    bizId, "owned_pages", userToken, cancellationToken)
                    ?? await TryResolveFromBusinessPageListAsync(
                        bizId, "client_pages", userToken, cancellationToken);

                if (resolved != null)
                    return resolved;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Business portfolio page fallback failed");
        }

        return null;
    }

    private async Task<(string PageId, string PageToken, string IgUserId)?> TryResolveFromBusinessPageListAsync(
        string businessId,
        string edge,
        string userToken,
        CancellationToken cancellationToken)
    {
        var pages = await GetJsonAsync(
            $"{_settings.GraphApiVersion}/{businessId}/{edge}?fields=id,name,instagram_business_account&access_token={userToken}",
            cancellationToken);

        if (!pages.TryGetProperty("data", out var data))
            return null;

        foreach (var page in data.EnumerateArray())
        {
            if (!page.TryGetProperty("instagram_business_account", out var ig)
                || ig.ValueKind != JsonValueKind.Object
                || !ig.TryGetProperty("id", out var igId))
            {
                continue;
            }

            var pageId = page.GetProperty("id").GetString()!;
            var pageToken = await FetchPageAccessTokenAsync(pageId, userToken, cancellationToken);
            if (pageToken == null)
                continue;

            _logger.LogInformation(
                "Resolved Instagram via business {Edge} for page {PageId}", edge, pageId);
            return (pageId, pageToken, igId.GetString()!);
        }

        return null;
    }

    private async Task<string?> FetchPageAccessTokenAsync(
        string pageId,
        string userToken,
        CancellationToken cancellationToken)
    {
        try
        {
            var pageInfo = await GetJsonAsync(
                $"{_settings.GraphApiVersion}/{pageId}?fields=access_token&access_token={userToken}",
                cancellationToken);
            return pageInfo.TryGetProperty("access_token", out var tok) ? tok.GetString() : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not fetch page access token for {PageId}", pageId);
            return null;
        }
    }

    private async Task<string> ExchangeCodeForTokenAsync(string code, CancellationToken cancellationToken)
    {
        var url =
            $"{_settings.GraphApiVersion}/oauth/access_token" +
            $"?client_id={_settings.AppId}" +
            $"&redirect_uri={Uri.EscapeDataString(_settings.RedirectUri)}" +
            $"&client_secret={_settings.AppSecret}" +
            $"&code={Uri.EscapeDataString(code)}";

        using var doc = await GetJsonDocumentAsync(url, cancellationToken);
        return doc.RootElement.GetProperty("access_token").GetString()!;
    }

    private async Task<string> ExchangeForLongLivedTokenAsync(string shortToken, CancellationToken cancellationToken)
    {
        var url =
            $"{_settings.GraphApiVersion}/oauth/access_token" +
            $"?grant_type=fb_exchange_token" +
            $"&client_id={_settings.AppId}" +
            $"&client_secret={_settings.AppSecret}" +
            $"&fb_exchange_token={shortToken}";

        using var doc = await GetJsonDocumentAsync(url, cancellationToken);
        return doc.RootElement.GetProperty("access_token").GetString()!;
    }

    private async Task<JsonElement> GetJsonAsync(string path, CancellationToken cancellationToken)
    {
        using var doc = await GetJsonDocumentAsync($"https://graph.facebook.com/{path}", cancellationToken);
        return doc.RootElement.Clone();
    }

    private async Task<JsonDocument> GetJsonDocumentAsync(string url, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Meta API error: {body}");
        return JsonDocument.Parse(body);
    }
}
