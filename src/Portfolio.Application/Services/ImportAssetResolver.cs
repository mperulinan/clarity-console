using MediatR;
using Microsoft.Extensions.Logging;
using Portfolio.Application.CQRS.Commands;
using Portfolio.Application.DTOs;
using Portfolio.Application.Interfaces;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;

namespace Portfolio.Application.Services;

/// <summary>
/// Resolves asset IDs for imported transaction symbols, auto-creating missing stock assets via SyncAssetCommand.
/// </summary>
public class ImportAssetResolver(
    IUnitOfWork uow,
    IMediator mediator,
    ILogger<ImportAssetResolver> logger) : IImportAssetResolver
{
    public async Task<IReadOnlyDictionary<string, Guid>> ResolveMissingAssetsAsync(
        IEnumerable<string> symbols, 
        CancellationToken cancellationToken = default)
    {
        var distinctSymbols = symbols
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var symbolToAssetId = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        foreach (var symbol in distinctSymbols)
        {
            var existingAsset = await uow.Assets.GetBySymbolAsync(symbol, AssetType.Stock, cancellationToken);
            if (existingAsset == null)
            {
                logger.LogInformation("Auto-creating asset for symbol '{Symbol}'", symbol);
                await mediator.Send(new SyncAssetCommand(new AssetDto
                {
                    Symbol = symbol,
                    Type = AssetType.Stock.Value,
                    Name = symbol,
                    ExternalId = symbol
                }), cancellationToken);

                existingAsset = await uow.Assets.GetBySymbolAsync(symbol, AssetType.Stock, cancellationToken);
            }

            if (existingAsset != null)
            {
                symbolToAssetId[symbol] = existingAsset.Id;
            }
            else
            {
                logger.LogWarning("Failed to resolve newly synced asset for symbol '{Symbol}'", symbol);
            }
        }

        return symbolToAssetId;
    }
}

