using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SalonPro.Infrastructure.Storage;

namespace SalonPro.API.Controllers;

/// <summary>Fallback image serving when static file middleware is unavailable on host.</summary>
[ApiController]
[Route("media/social")]
[AllowAnonymous]
public class SocialMediaController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly SocialStorageSettings _settings;

    public SocialMediaController(IWebHostEnvironment environment, IOptions<SocialStorageSettings> settings)
    {
        _environment = environment;
        _settings = settings.Value;
    }

    [HttpGet("{tenantId}/{postId}.png")]
    [Produces("image/png")]
    public IActionResult GetImage(string tenantId, string postId)
    {
        if (!Guid.TryParseExact(tenantId, "N", out _) && !Guid.TryParse(tenantId, out _))
            return NotFound();

        if (!Guid.TryParseExact(postId, "N", out _) && !Guid.TryParse(postId, out _))
            return NotFound();

        var tenantFolder = tenantId.Replace("-", "");
        var fileName = postId.Contains('-') ? $"{Guid.Parse(postId):N}.png" : $"{postId}.png";

        var root = LocalMediaStorageService.GetPhysicalRoot(_environment, _settings);
        var fullPath = Path.Combine(root, tenantFolder, fileName);

        if (!System.IO.File.Exists(fullPath))
            return NotFound();

        return PhysicalFile(fullPath, "image/png", enableRangeProcessing: true);
    }
}
