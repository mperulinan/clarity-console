using System;
using MediatR;

namespace Portfolio.Application.CQRS.Queries;

public record GetAssetSpotPriceQuery(Guid AssetId, string FiatCurrency, DateTime? Date) : IRequest<decimal?>;
