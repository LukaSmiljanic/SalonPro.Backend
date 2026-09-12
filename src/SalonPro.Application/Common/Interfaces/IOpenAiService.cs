namespace SalonPro.Application.Common.Interfaces;

public interface IOpenAiService
{
    bool Enabled { get; }
    bool HasApiKey { get; }
    int ApiKeyLength { get; }
    string KeySource { get; }
    bool IsConfigured { get; }
    bool ImageGenerationEnabled { get; }
    string? LastError { get; }

    Task<string?> GenerateChatJsonAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default);

    Task<string?> GenerateChatJsonWithImageAsync(
        string systemPrompt,
        string userPrompt,
        byte[] imageBytes,
        string imageContentType,
        CancellationToken cancellationToken = default);

    Task<byte[]?> GenerateImageAsync(string prompt, CancellationToken cancellationToken = default);

    /// <summary>Reference-based image variant (preserves branding via input_fidelity).</summary>
    Task<byte[]?> EditImageAsync(
        byte[] imageBytes,
        string imageContentType,
        string prompt,
        CancellationToken cancellationToken = default);
}
