using Microsoft.AspNetCore.Mvc;
using SalonPro.Application.Features.Dashboard.DTOs;
using SalonPro.Application.Features.Dashboard.Queries.GetBirthdayReminders;
using SalonPro.Application.Features.Dashboard.Queries.GetDashboardStats;
using SalonPro.Application.Features.Dashboard.Queries.GetPopularServices;
using SalonPro.Application.Features.Dashboard.Queries.GetRevenueChart;
using SalonPro.Application.Features.Insights.DTOs;
using SalonPro.Application.Features.Insights.Queries.GetDashboardInsights;

namespace SalonPro.API.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController : ApiControllerBase
{
    [HttpGet("stats")]
    [ProducesResponseType(typeof(DashboardStatsDto), 200)]
    public async Task<IActionResult> GetStats([FromQuery] DateTime? date = null)
    {
        var result = await Mediator.Send(new GetDashboardStatsQuery(date));
        return Ok(result);
    }

    [HttpGet("revenue-chart")]
    [ProducesResponseType(typeof(RevenueChartDto), 200)]
    public async Task<IActionResult> GetRevenueChart([FromQuery] ChartPeriod period = ChartPeriod.Week, [FromQuery] int? days = null)
    {
        var result = await Mediator.Send(new GetRevenueChartQuery(period, days));
        return Ok(result);
    }

    [HttpGet("popular-services")]
    [ProducesResponseType(typeof(List<PopularServiceDto>), 200)]
    public async Task<IActionResult> GetPopularServices([FromQuery] ChartPeriod period = ChartPeriod.Week)
    {
        var result = await Mediator.Send(new GetPopularServicesQuery(period));
        return Ok(result);
    }

    [HttpGet("birthday-reminders")]
    [ProducesResponseType(typeof(List<BirthdayReminderDto>), 200)]
    public async Task<IActionResult> GetBirthdayReminders([FromQuery] int days = 7)
    {
        var result = await Mediator.Send(new GetBirthdayRemindersQuery(days));
        return Ok(result);
    }

    [HttpGet("insights")]
    [ProducesResponseType(typeof(DashboardInsightsDto), 200)]
    public async Task<IActionResult> GetDashboardInsights()
    {
        var result = await Mediator.Send(new GetDashboardInsightsQuery());
        return Ok(result);
    }
}
