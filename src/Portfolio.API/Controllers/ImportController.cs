using MediatR;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.CQRS.Commands;
using Portfolio.Application.DTOs;

namespace Portfolio.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ImportController(ISender sender) : ControllerBase
{
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
            new CommitImportTransactionsCommand(brokerKey, request.PreviewToken, request.ExcludedBrokerTransactionIds),
            cancellationToken);

        return Ok(result);
    }
}
