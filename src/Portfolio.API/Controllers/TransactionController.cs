using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.Interfaces;
using Portfolio.Infrastructure.Persistence;
using Portfolio.Application.DTOs;
using Portfolio.Domain.Enums;

namespace Portfolio.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TransactionController(IPortfolioService portfolioService) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<TransactionDto>> GetTransactions()
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

    [HttpGet("types")]
    public ActionResult<IEnumerable<TransactionTypeDto>> GetTransactionTypes()
    {
        var types = TransactionType.List.Select(t => new TransactionTypeDto
        { 
            Value = t.Value, 
            Label = t.Name,
            RequiresFromAsset = t.RequiresFromAsset,
            RequiresToAsset = t.RequiresToAsset
        });
        return Ok(types);
    }
}
