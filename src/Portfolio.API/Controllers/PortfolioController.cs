using MediatR;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.DTOs;
using Portfolio.Application.Interfaces;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PortfolioController(IPortfolioService portfolioService, ISender sender) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<ActionResult<PortfolioMetrics>> GetDashboard()
    {
        var metrics = await portfolioService.GetPortfolioMetricsAsync();
        return Ok(metrics);
    }

    [HttpGet("tax-report")]
    public async Task<ActionResult<PortfolioReportDto>> GetTaxReport()
    {
        var report = await portfolioService.GetPortfolioReportAsync();
        return Ok(report);
    }
}
