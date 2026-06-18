using MediatR;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.CQRS.Commands;
using Portfolio.Application.CQRS.Queries;
using Portfolio.Application.DTOs;
using Portfolio.Domain.Enums;

namespace Portfolio.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TransactionController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<TransactionDto>> GetTransactions()
    {
        var report = await sender.Send(new GetPortfolioReportQuery());
        return report.Transactions.Select(t => t.Transaction);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TransactionDto>> GetTransaction(int id)
    {
        var result = await sender.Send(new GetTransactionByIdQuery(id));
        if (result is null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult> PostTransaction(NewTransactionRequest request)
    {
        var result = await sender.Send(new AddTransactionCommand(request));
        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> PutTransaction(int id, NewTransactionRequest request)
    {
        var result = await sender.Send(new UpdateTransactionCommand(id, request));
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteTransaction(int id)
    {
        await sender.Send(new DeleteTransactionCommand(id));
        return NoContent();
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
