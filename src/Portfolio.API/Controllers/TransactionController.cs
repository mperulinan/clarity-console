using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.Interfaces;
using Portfolio.Infrastructure.Persistence;
using Portfolio.Application.DTOs;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TransactionController(IPortfolioService portfolioService) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<Domain.Entities.Transaction>> GetTransactions()
    {
        var report = await portfolioService.GetPortfolioReportAsync();
        return report.Transactions.Select(t => t.Transaction);
    }

    [HttpPost]
    public async Task<ActionResult> PostTransaction(NewTransactionRequest request)
    {
        await portfolioService.AddTransactionAsync(request);
        return Ok();
    }
}
