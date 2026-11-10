using MediatR;
using Microsoft.Extensions.Logging;
using Portfolio.Application.CQRS.Commands;
using Portfolio.Application.DTOs;
using Portfolio.Application.Interfaces;
using Portfolio.Application.Models;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;

namespace Portfolio.Application.Services;

/// <summary>
/// Resolves asset IDs for imported transaction symbols, auto-creating missing stock or ETF assets via SyncAssetCommand.
/// </summary>
public class ImportAssetResolver(
    IUnitOfWork uow,
    IMediator mediator,
    ILogger<ImportAssetResolver> logger) : IImportAssetResolver
{
    public async Task<IReadOnlyDictionary<string, Guid>> ResolveMissingAssetsAsync(
        IEnumerable<ImportAssetToResolve> assets, 
        CancellationToken cancellationToken = default)
    {
        var distinctAssets = assets
            .Where(a => !string.IsNullOrWhiteSpace(a.Symbol))
            .GroupBy(a => a.Symbol, StringComparer.OrdinalIgnoreCase)
            .Select(g => new ImportAssetToResolve(
                g.Key, 
                g.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.AssetType))?.AssetType
            ))
            .ToList();

        var symbolToAssetId = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in distinctAssets)
        {
            var symbol = item.Symbol;
            AssetType? requestedType = null;
            if (!string.IsNullOrWhiteSpace(item.AssetType) &&
                (AssetType.TryFromValue(item.AssetType, out var byVal) || AssetType.TryFromName(item.AssetType, true, out byVal)))
            {
                requestedType = byVal;
            }

            // Prefer resolving with the type from the caller when provided; fallback order if missing: Stock then Etf
            Asset? existingAsset;
            if (requestedType != null)
            {
                existingAsset = await uow.Assets.GetBySymbolAsync(symbol, requestedType, cancellationToken)
                    ?? await uow.Assets.GetBySymbolAsync(symbol, requestedType == AssetType.Stock ? AssetType.Etf : AssetType.Stock, cancellationToken);
            }
            else
            {
                existingAsset = await uow.Assets.GetBySymbolAsync(symbol, AssetType.Stock, cancellationToken)
                    ?? await uow.Assets.GetBySymbolAsync(symbol, AssetType.Etf, cancellationToken);
            }

            if (existingAsset == null)
            {
                // Auto-create using provided type, or default to Stock if type is unknown.
                var targetType = (requestedType == AssetType.Etf) ? AssetType.Etf : AssetType.Stock;

                logger.LogInformation("Auto-creating asset for symbol '{Symbol}' with type '{Type}'", symbol, targetType.Value);
                await mediator.Send(new SyncAssetCommand(new AssetDto
                {
                    Symbol = symbol,
                    Type = targetType.Value,
                    Name = symbol,
                    ExternalId = symbol
                }), cancellationToken);

                existingAsset = await uow.Assets.GetBySymbolAsync(symbol, targetType, cancellationToken);
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

    public Task<IReadOnlyDictionary<string, Guid>> ResolveMissingAssetsAsync(
        IEnumerable<string> symbols, 
        CancellationToken cancellationToken = default) =>
        ResolveMissingAssetsAsync(symbols.Select(s => new ImportAssetToResolve(s)), cancellationToken);
}
