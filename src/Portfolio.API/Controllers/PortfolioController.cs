using MediatR;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.CQRS.Queries;
using Portfolio.Application.DTOs;

namespace Portfolio.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PortfolioController(ISender sender) : ControllerBase
{
    [HttpGet("metrics")]
    public async Task<ActionResult<PortfolioMetricsDto>> GetMetrics() =>
        Ok(await sender.Send(new GetPortfolioMetricsQuery()));

    [HttpGet("tax-report")]
    public async Task<ActionResult<PortfolioReportDto>> GetTaxReport() =>
        Ok(await sender.Send(new GetPortfolioReportQuery()));
}
