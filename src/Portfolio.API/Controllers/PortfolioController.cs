using MediatR;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.CQRS.Queries;
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
        var result = await sender.Send(new GetPortfolioMetricsQuery());
        return Ok(result);
    }

    [HttpGet("tax-report")]
    public async Task<ActionResult<PortfolioReportDto>> GetTaxReport()
    {
        var result = await sender.Send(new GetPortfolioReportQuery());
        return Ok(result);
    }
}
