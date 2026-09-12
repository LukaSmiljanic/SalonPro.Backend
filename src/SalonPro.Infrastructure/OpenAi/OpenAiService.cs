using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SalonPro.Application.Common.Interfaces;

namespace SalonPro.Infrastructure.OpenAi;

public class OpenAiService : IOpenAiService
{
    private readonly HttpClient _httpClient;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OpenAiSettings _settings;
    private readonly ILogger<OpenAiService> _logger;
    private string? _lastError;

    public OpenAiService(
        HttpClient httpClient,
        IHttpClientFactory httpClientFactory,
        IOptions<OpenAiSettings> settings,
        ILogger<OpenAiService> logger)
    {
        _httpClient = httpClient;
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    public bool Enabled => _settings.Enabled;
    public bool HasApiKey => !string.IsNullOrWhiteSpace(_settings.ApiKey);
    public int ApiKeyLength => _settings.ApiKey?.Trim().Length ?? 0;
    public string KeySource => _settings.KeySource;
    public bool IsConfigured => Enabled && HasApiKey;
    public bool ImageGenerationEnabled => IsConfigured && _settings.GenerateImages;
    public string? LastError => _lastError;

    public async Task<string?> GenerateChatJsonAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            _lastError = "OpenAI ApiKey nije podešen na serveru (OpenAI:ApiKey ili OpenAI__ApiKey).";
            return null;
        }

        var payload = new
        {
            model = _settings.TextModel,
            temperature = 0.8,
            response_format = new { type = "json_object" },
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt },
            },
        };

        using var request = CreateRequest(HttpMethod.Post, "chat/completions", payload);
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _lastError = $"OpenAI mreža: {ex.Message}";
            _logger.LogWarning(ex, "OpenAI chat network error");
            return null;
        }

        using (response)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _lastError = ParseOpenAiError(body, response.StatusCode);
                _logger.LogWarning("OpenAI chat failed: {Status} {Body}", response.StatusCode, body);
                return null;
            }

            _lastError = null;
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();
        }
    }

    public async Task<string?> GenerateChatJsonWithImageAsync(
        string systemPrompt,
        string userPrompt,
        byte[] imageBytes,
        string imageContentType,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            _lastError = "OpenAI ApiKey nije podešen na serveru (OpenAI:ApiKey ili OpenAI__ApiKey).";
            return null;
        }

        if (imageBytes.Length == 0)
        {
            _lastError = "Slika je prazna.";
            return null;
        }

        var mime = NormalizeImageMime(imageContentType);
        var dataUrl = $"data:{mime};base64,{Convert.ToBase64String(imageBytes)}";

        var payload = new
        {
            model = _settings.TextModel,
            temperature = 0.7,
            response_format = new { type = "json_object" },
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = userPrompt },
                        new { type = "image_url", image_url = new { url = dataUrl, detail = "high" } },
                    },
                },
            },
        };

        using var request = CreateRequest(HttpMethod.Post, "chat/completions", payload);
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _lastError = $"OpenAI mreža: {ex.Message}";
            _logger.LogWarning(ex, "OpenAI vision chat network error");
            return null;
        }

        using (response)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _lastError = ParseOpenAiError(body, response.StatusCode);
                _logger.LogWarning("OpenAI vision chat failed: {Status} {Body}", response.StatusCode, body);
                return null;
            }

            _lastError = null;
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();
        }
    }

    public async Task<byte[]?> GenerateImageAsync(string prompt, CancellationToken cancellationToken = default)
    {
        if (!ImageGenerationEnabled)
        {
            _lastError = IsConfigured
                ? "GenerateImages je isključen u konfiguraciji."
                : "OpenAI ApiKey nije podešen na serveru.";
            return null;
        }

        var payload = BuildImagePayload(prompt);

        using var request = CreateRequest(HttpMethod.Post, "images/generations", payload);
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _lastError = $"OpenAI mreža: {ex.Message}";
            _logger.LogWarning(ex, "OpenAI image network error");
            return null;
        }

        using (response)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _lastError = ParseOpenAiError(body, response.StatusCode);
                _logger.LogWarning("OpenAI image failed: {Status} {Body}", response.StatusCode, body);
                return null;
            }

            _lastError = null;
            return await ExtractImageBytesFromResponse(body, cancellationToken);
        }
    }

    public async Task<byte[]?> EditImageAsync(
        byte[] imageBytes,
        string imageContentType,
        string prompt,
        CancellationToken cancellationToken = default)
    {
        if (!ImageGenerationEnabled)
        {
            _lastError = IsConfigured
                ? "GenerateImages je isključen u konfiguraciji."
                : "OpenAI ApiKey nije podešen na serveru.";
            return null;
        }

        if (imageBytes.Length == 0)
        {
            _lastError = "Referentna slika je prazna.";
            return null;
        }

        var model = ResolveImageModelForEdit();
        var quality = NormalizeImageQuality(_settings.ImageQuality);
        var mime = NormalizeImageMime(imageContentType);
        var extension = mime switch
        {
            "image/png" => "png",
            "image/webp" => "webp",
            _ => "jpeg",
        };

        using var form = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(imageBytes);
        imageContent.Headers.ContentType = new MediaTypeHeaderValue(mime);
        form.Add(imageContent, "image", $"reference.{extension}");
        form.Add(new StringContent(prompt), "prompt");
        form.Add(new StringContent(model), "model");
        form.Add(new StringContent("1024x1024"), "size");
        form.Add(new StringContent(quality), "quality");
        form.Add(new StringContent("high"), "input_fidelity");

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/images/edits");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey.Trim());
        request.Content = form;

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _lastError = $"OpenAI mreža: {ex.Message}";
            _logger.LogWarning(ex, "OpenAI image edit network error");
            return null;
        }

        using (response)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _lastError = ParseOpenAiError(body, response.StatusCode);
                _logger.LogWarning("OpenAI image edit failed: {Status} {Body}", response.StatusCode, body);
                return null;
            }

            _lastError = null;
            return await ExtractImageBytesFromResponse(body, cancellationToken);
        }
    }

    private async Task<byte[]?> ExtractImageBytesFromResponse(string body, CancellationToken cancellationToken)
    {
        using var doc = JsonDocument.Parse(body);
        var data0 = doc.RootElement.GetProperty("data")[0];

        if (data0.TryGetProperty("b64_json", out var b64El))
        {
            var b64 = b64El.GetString();
            if (!string.IsNullOrWhiteSpace(b64))
                return Convert.FromBase64String(b64);
        }

        var url = data0.TryGetProperty("url", out var urlEl) ? urlEl.GetString() : null;
        if (string.IsNullOrWhiteSpace(url))
        {
            _lastError = "OpenAI nije vratio URL slike.";
            return null;
        }

        try
        {
            var fetchClient = _httpClientFactory.CreateClient("OpenAiImageFetch");
            return await fetchClient.GetByteArrayAsync(url, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _lastError = $"Preuzimanje slike sa OpenAI: {ex.Message}";
            return null;
        }
    }

    private object BuildImagePayload(string prompt)
    {
        var model = ResolveImageModel();
        var quality = NormalizeImageQuality(_settings.ImageQuality);

        // GPT Image API — b64_json in response by default; no response_format / n params.
        return new
        {
            model,
            prompt,
            size = "1024x1024",
            quality,
        };
    }

    private string ResolveImageModel()
    {
        var configured = _settings.ImageModel?.Trim() ?? string.Empty;
        if (configured.StartsWith("gpt-image", StringComparison.OrdinalIgnoreCase))
            return configured;

        // Auto-migrate legacy dall-e-* settings on Monster after OpenAI deprecation.
        _logger.LogWarning(
            "ImageModel '{Model}' is deprecated — using gpt-image-1-mini",
            string.IsNullOrEmpty(configured) ? "(empty)" : configured);
        return "gpt-image-1-mini";
    }

    private string ResolveImageModelForEdit()
    {
        var configured = _settings.ImageModel?.Trim() ?? string.Empty;
        if (configured.Equals("gpt-image-1-mini", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrEmpty(configured)
            || configured.StartsWith("dall-e", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Using gpt-image-1 for edits (input_fidelity requires full model).");
            return "gpt-image-1";
        }

        return configured;
    }

    private static string NormalizeImageMime(string? contentType) =>
        contentType?.Trim().ToLowerInvariant() switch
        {
            "image/png" => "image/png",
            "image/webp" => "image/webp",
            "image/jpg" or "image/jpeg" => "image/jpeg",
            _ => "image/jpeg",
        };

    private static string NormalizeImageQuality(string? quality) =>
        quality?.Trim().ToLowerInvariant() switch
        {
            "medium" or "high" or "low" or "auto" => quality.Trim().ToLowerInvariant(),
            _ => "low",
        };

    private static string ParseOpenAiError(string body, System.Net.HttpStatusCode status)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var err)
                && err.TryGetProperty("message", out var msg))
                return $"OpenAI ({(int)status}): {msg.GetString()}";
        }
        catch { /* ignore */ }

        return $"OpenAI ({(int)status}): {body[..Math.Min(body.Length, 200)]}";
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path, object payload)
    {
        var request = new HttpRequestMessage(method, $"https://api.openai.com/v1/{path}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey.Trim());
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");
        return request;
    }
}
