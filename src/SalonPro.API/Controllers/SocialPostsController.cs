using Microsoft.AspNetCore.Mvc;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Application.Features.Social.Commands.DeleteSocialPost;
using SalonPro.Application.Features.Social.Commands.EnsureSocialSchema;
using SalonPro.Application.Features.Social.Commands.GenerateSocialWeek;
using SalonPro.Application.Features.Social.Commands.RegeneratePostImage;
using SalonPro.Application.Features.Social.Commands.PublishSocialPostNow;
using SalonPro.Application.Features.Social.Commands.ScheduleSocialPost;
using SalonPro.Application.Features.Social.Commands.UpdateSocialPost;
using SalonPro.Application.Features.Social.DTOs;
using SalonPro.Application.Common.Exceptions;
using SalonPro.Application.Features.Social.Queries.GetSocialDbDiagnostics;
using SalonPro.Application.Features.Social.Queries.GetSocialConfig;
using SalonPro.Application.Features.Social.Queries.GetSocialPosts;
using SalonPro.Application.Features.Social.Queries.TestOpenAi;
using SalonPro.Application.Features.Social.Queries.TestOpenAiImage;

namespace SalonPro.API.Controllers;

[Route("api/social-posts")]
public class SocialPostsController : ApiControllerBase
{
    [HttpGet("config")]
    [ProducesResponseType(typeof(SocialConfigDto), 200)]
    public async Task<IActionResult> GetConfig()
    {
        var result = await Mediator.Send(new GetSocialConfigQuery());
        return Ok(result);
    }

    [HttpGet("db-diagnostics")]
    [ProducesResponseType(typeof(SocialDbDiagnosticsDto), 200)]
    public async Task<IActionResult> GetDbDiagnostics()
    {
        var result = await Mediator.Send(new GetSocialDbDiagnosticsQuery());
        return Ok(result);
    }

    [HttpPost("ensure-schema")]
    [ProducesResponseType(typeof(SocialSchemaBootstrapResult), 200)]
    public async Task<IActionResult> EnsureSchema()
    {
        var result = await Mediator.Send(new EnsureSocialSchemaCommand());
        return Ok(result);
    }

    [HttpPost("test-openai")]
    [ProducesResponseType(typeof(OpenAiTestResultDto), 200)]
    public async Task<IActionResult> TestOpenAi()
    {
        var result = await Mediator.Send(new TestOpenAiQuery());
        return Ok(result);
    }

    [HttpPost("test-openai-image")]
    [ProducesResponseType(typeof(OpenAiImageTestResultDto), 200)]
    public async Task<IActionResult> TestOpenAiImage()
    {
        var result = await Mediator.Send(new TestOpenAiImageQuery());
        return Ok(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(SocialPostsWeekDto), 200)]
    public async Task<IActionResult> GetPosts(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var result = await Mediator.Send(new GetSocialPostsQuery(from, to));
        return Ok(result);
    }

    [HttpPost("generate-week")]
    [ProducesResponseType(typeof(SocialPostsWeekDto), 200)]
    public async Task<IActionResult> GenerateWeek([FromBody] GenerateWeekRequest? body)
    {
        var result = await Mediator.Send(new GenerateSocialWeekCommand(body?.WeekStartLocalDate));
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(SocialPostDto), 200)]
    public async Task<IActionResult> UpdatePost([FromRoute] Guid id, [FromBody] UpdateSocialPostRequest body)
    {
        var result = await Mediator.Send(new UpdateSocialPostCommand(
            id,
            body.Caption,
            body.Hashtags,
            body.ScheduledAt));
        return Ok(result);
    }

    [HttpPost("{id:guid}/schedule")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> SchedulePost([FromRoute] Guid id)
    {
        await Mediator.Send(new ScheduleSocialPostCommand(id));
        return NoContent();
    }

    [HttpPost("{id:guid}/publish-now")]
    [ProducesResponseType(typeof(SocialPostPublishResult), 200)]
    public async Task<IActionResult> PublishNow([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new PublishSocialPostNowCommand(id));
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> DeletePost([FromRoute] Guid id)
    {
        await Mediator.Send(new DeleteSocialPostCommand(id));
        return NoContent();
    }

    [HttpPost("{id:guid}/regenerate-image")]
    [ProducesResponseType(typeof(RegenerateImageQueuedDto), 202)]
    public async Task<IActionResult> RegenerateImage([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new RegeneratePostImageCommand(id));
        return Accepted(result);
    }

    public record GenerateWeekRequest(DateTime? WeekStartLocalDate);
    public record UpdateSocialPostRequest(string Caption, string Hashtags, DateTime ScheduledAt);
}
