using MediatR;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;

namespace Portfolio.Application.CQRS.Queries;

public class GetAssetSpotPriceQueryHandler(
    IUnitOfWork unitOfWork,
    IAssetPriceProviderFactory priceProviderFactory) : IRequestHandler<GetAssetSpotPriceQuery, decimal?>
{
    public async Task<decimal?> Handle(GetAssetSpotPriceQuery request, CancellationToken cancellationToken)
    {
        if (!FiatCurrency.TryFromValue(request.FiatCurrency.ToLowerInvariant(), out var currency))
        {
            throw new ArgumentException("Invalid fiat currency.");
        }

        var asset = await unitOfWork.Assets.GetByIdAsync(request.AssetId);
        if (asset == null || string.IsNullOrWhiteSpace(asset.ExternalId))
        {
            return null;
        }

        var priceProvider = priceProviderFactory.GetProvider(asset.Type);
        if (priceProvider == null)
        {
            throw new ArgumentException($"No price provider available for asset type '{asset.Type.Name}'.");
        }

        // Only fetch historical prices if the requested UTC date is strictly in the past.
        // If it's today (or future due to timezones), fetch the live spot price.
        if (request.Date.HasValue)
        {
            var isHistorical = request.Date.Value.Date < DateTime.UtcNow.Date;
            if (isHistorical)
            {
                return await priceProvider.GetHistoricalPriceAsync(asset.ExternalId, currency, request.Date.Value);
            }
        }

        var prices = await priceProvider.GetPricesAsync([asset.ExternalId], currency);
        if (prices.TryGetValue(asset.ExternalId, out var price))
        {
            return price;
        }

        return null;
    }
}
