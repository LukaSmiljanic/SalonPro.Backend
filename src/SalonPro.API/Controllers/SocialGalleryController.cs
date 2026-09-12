using Microsoft.AspNetCore.Mvc;
using SalonPro.Application.Features.Social.Commands.CreatePostFromGallery;
using SalonPro.Application.Features.Social.Commands.CreateGalleryAiVariant;
using SalonPro.Application.Features.Social.Commands.DeleteSocialGalleryImage;
using SalonPro.Application.Features.Social.Commands.UploadSocialGalleryImage;
using SalonPro.Application.Features.Social.DTOs;
using SalonPro.Application.Features.Social.Queries.GetSocialGallery;

namespace SalonPro.API.Controllers;

[Route("api/social/gallery")]
public class SocialGalleryController : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SocialGalleryImageDto>), 200)]
    public async Task<IActionResult> List()
    {
        var result = await Mediator.Send(new GetSocialGalleryQuery());
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(SocialGalleryImageDto), 200)]
    [ProducesResponseType(400)]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Upload(
        IFormFile file,
        [FromForm] string? captionHint,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { detail = "Izaberite sliku za upload." });

        await using var stream = file.OpenReadStream();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken);

        var result = await Mediator.Send(new UploadSocialGalleryImageCommand(
            file.FileName,
            ms.ToArray(),
            file.ContentType ?? "image/jpeg",
            captionHint), cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        await Mediator.Send(new DeleteSocialGalleryImageCommand(id));
        return NoContent();
    }

    [HttpPost("{id:guid}/create-post")]
    [ProducesResponseType(typeof(CreatePostFromGalleryResult), 200)]
    public async Task<IActionResult> CreatePost(
        [FromRoute] Guid id,
        [FromBody] CreatePostFromGalleryRequest? body)
    {
        var result = await Mediator.Send(new CreatePostFromGalleryCommand(id, body?.CaptionHint));
        return Ok(result);
    }

    public record CreatePostFromGalleryRequest(string? CaptionHint);

    [HttpPost("{id:guid}/ai-variant")]
    [ProducesResponseType(typeof(CreateGalleryAiVariantResult), 200)]
    public async Task<IActionResult> CreateAiVariant(
        [FromRoute] Guid id,
        [FromBody] CreateGalleryAiVariantRequest body,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(body.Prompt))
            return BadRequest(new { detail = "Unesite prompt za AI varijantu." });

        var result = await Mediator.Send(
            new CreateGalleryAiVariantCommand(id, body.Prompt, body.CaptionHint),
            cancellationToken);
        return Ok(result);
    }
}
