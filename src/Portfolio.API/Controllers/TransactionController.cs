using MediatR;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.DTOs;
using Portfolio.Application.Interfaces;
using Portfolio.Domain.Enums;

namespace Portfolio.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TransactionController(IPortfolioService portfolioService, ISender sender) : ControllerBase
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
            Name = t.Name,
            RequiresFromAsset = t.RequiresFromAsset,
            RequiresToAsset = t.RequiresToAsset
        });
        return Ok(types);
    }
}
