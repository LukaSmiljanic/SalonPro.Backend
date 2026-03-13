using Microsoft.AspNetCore.Mvc;
using SalonPro.Application.Features.Reports.DTOs;
using SalonPro.Application.Features.Reports.Queries.GetReportSummary;
using SalonPro.Application.Features.Reports.Queries.GetRevenueByService;
using SalonPro.Application.Features.Reports.Queries.GetRevenueByStaff;

namespace SalonPro.API.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController : ApiControllerBase
{
    [HttpGet("revenue-by-staff")]
    [ProducesResponseType(typeof(List<StaffRevenueDto>), 200)]
    public async Task<IActionResult> GetRevenueByStaff(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to)
    {
        var result = await Mediator.Send(new GetRevenueByStaffQuery(from, to));
        return Ok(result);
    }

    [HttpGet("revenue-by-service")]
    [ProducesResponseType(typeof(List<ServiceRevenueDto>), 200)]
    public async Task<IActionResult> GetRevenueByService(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to)
    {
        var result = await Mediator.Send(new GetRevenueByServiceQuery(from, to));
        return Ok(result);
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(ReportSummaryDto), 200)]
    public async Task<IActionResult> GetSummary(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to)
    {
        var result = await Mediator.Send(new GetReportSummaryQuery(from, to));
        return Ok(result);
    }
}

