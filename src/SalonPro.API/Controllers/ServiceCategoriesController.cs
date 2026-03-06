using Microsoft.AspNetCore.Mvc;
using SalonPro.Application.Features.ServiceCategories.Commands.CreateServiceCategory;
using SalonPro.Application.Features.ServiceCategories.DTOs;
using SalonPro.Application.Features.ServiceCategories.Queries.GetServiceCategories;

namespace SalonPro.API.Controllers;

[ApiController]
[Route("api/service-categories")]
public class ServiceCategoriesController : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<ServiceCategoryDto>), 200)]
    public async Task<IActionResult> GetCategories([FromQuery] bool includeInactive = false)
    {
        var result = await Mediator.Send(new GetServiceCategoriesQuery(includeInactive));
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateCategory([FromBody] CreateServiceCategoryCommand command)
    {
        var id = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetCategories), new { id }, id);
    }
}
