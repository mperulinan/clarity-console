using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.Interfaces;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PortfolioController(IPortfolioService portfolioService) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<ActionResult<PortfolioMetrics>> GetDashboard()
    {
        var metrics = await portfolioService.GetPortfolioMetricsAsync();
        return Ok(metrics);
    }

    [HttpGet("tax-report")]
    public async Task<ActionResult<PortfolioReport>> GetTaxReport()
    {
        var report = await portfolioService.GetPortfolioReportAsync();
        return Ok(report);
    }
}
