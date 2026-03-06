using Microsoft.AspNetCore.Mvc;
using SalonPro.Application.Features.Services.Commands.CreateService;
using SalonPro.Application.Features.Services.Commands.DeleteService;
using SalonPro.Application.Features.Services.Commands.UpdateService;
using SalonPro.Application.Features.Services.DTOs;
using SalonPro.Application.Features.Services.Queries.GetServices;
using SalonPro.Application.Features.Services.Queries.GetServicesByCategory;

namespace SalonPro.API.Controllers;

[ApiController]
[Route("api/services")]
public class ServicesController : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<ServiceDto>), 200)]
    public async Task<IActionResult> GetServices([FromQuery] bool includeInactive = false)
    {
        var result = await Mediator.Send(new GetServicesQuery(includeInactive));
        return Ok(result);
    }

    [HttpGet("by-category/{categoryId:guid}")]
    [ProducesResponseType(typeof(List<ServiceDto>), 200)]
    public async Task<IActionResult> GetByCategory([FromRoute] Guid categoryId)
    {
        var result = await Mediator.Send(new GetServicesByCategoryQuery(categoryId));
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateService([FromBody] CreateServiceCommand command)
    {
        var id = await Mediator.Send(command);
        return Ok(id);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateService(
        [FromRoute] Guid id,
        [FromBody] UpdateServiceCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID mismatch.");

        await Mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteService([FromRoute] Guid id)
    {
        await Mediator.Send(new DeleteServiceCommand(id));
        return NoContent();
    }
}
