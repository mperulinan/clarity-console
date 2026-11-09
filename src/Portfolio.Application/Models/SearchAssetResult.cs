using Portfolio.Domain.Entities;

namespace Portfolio.Application.Models;

/// <summary>
/// Wraps a search result asset with transient metadata (e.g. market cap rank)
/// that should not be persisted on the domain entity.
/// </summary>
public record SearchAssetResult(Asset Asset, int? MarketCapRank);
