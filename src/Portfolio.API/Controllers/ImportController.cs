using MediatR;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.CQRS.Commands;
using Portfolio.Application.CQRS.Queries;
using Portfolio.Application.DTOs;

namespace Portfolio.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ImportController(ISender sender) : ControllerBase
{
    [HttpGet("brokers")]
    public async Task<ActionResult<IReadOnlyList<BrokerDto>>> GetBrokers(CancellationToken cancellationToken)
    {
        var brokers = await sender.Send(new GetAvailableBrokersQuery(), cancellationToken);
        return Ok(brokers);
    }

    [HttpPost("{brokerKey}/preview")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ImportTransactionsPreviewDto>> Preview(
        string brokerKey,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("A valid non-empty CSV file must be provided.");
        }

        using var stream = file.OpenReadStream();
        var result = await sender.Send(new PreviewImportTransactionsCommand(brokerKey, stream), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{brokerKey}/commit")]
    [Consumes("application/json")]
    public async Task<ActionResult<ImportTransactionsResultDto>> Commit(
        string brokerKey,
        [FromBody] CommitImportTransactionsRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.PreviewToken))
        {
            return BadRequest("A valid preview token must be provided.");
        }

        var result = await sender.Send(
            new CommitImportTransactionsCommand(brokerKey, request.PreviewToken, request.ExcludedBrokerTransactionIds, request.OverriddenSymbols),
            cancellationToken);

        return Ok(result);
    }
}
