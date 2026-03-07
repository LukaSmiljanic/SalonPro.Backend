using Microsoft.AspNetCore.Mvc;
using SalonPro.Application.Common.Models;
using SalonPro.Application.Features.Clients.Commands.CreateClient;
using SalonPro.Application.Features.Clients.Commands.DeleteClient;
using SalonPro.Application.Features.Clients.Commands.UpdateClient;
using SalonPro.Application.Features.Clients.DTOs;
using SalonPro.Application.Features.Clients.Queries.GetClientById;
using SalonPro.Application.Features.Clients.Queries.GetClientLoyalty;
using SalonPro.Application.Features.Clients.Queries.GetClients;
using SalonPro.Application.Features.Clients.Queries.SearchClients;
using SalonPro.Application.Features.Insights.DTOs;
using SalonPro.Application.Features.Insights.Queries.GetClientInsights;

namespace SalonPro.API.Controllers;

[ApiController]
[Route("api/clients")]
public class ClientsController : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<ClientListDto>), 200)]
    public async Task<IActionResult> GetClients(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null)
    {
        var result = await Mediator.Send(new GetClientsQuery(pageNumber, pageSize, searchTerm));
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ClientDetailDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetClient([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetClientByIdQuery(id));
        return Ok(result);
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(List<ClientListDto>), 200)]
    public async Task<IActionResult> SearchClients([FromQuery] string q)
    {
        var result = await Mediator.Send(new SearchClientsQuery(q));
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateClient([FromBody] CreateClientCommand command)
    {
        var id = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetClient), new { id }, id);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateClient(
        [FromRoute] Guid id,
        [FromBody] UpdateClientCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID mismatch.");

        await Mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteClient([FromRoute] Guid id)
    {
        await Mediator.Send(new DeleteClientCommand(id));
        return NoContent();
    }

    [HttpGet("{id:guid}/loyalty")]
    [ProducesResponseType(typeof(ClientLoyaltyDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetClientLoyalty([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetClientLoyaltyQuery(id));
        return Ok(result);
    }

    [HttpGet("{id:guid}/insights")]
    [ProducesResponseType(typeof(ClientInsightsDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetClientInsights([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetClientInsightsQuery(id));
        return Ok(result);
    }
}
