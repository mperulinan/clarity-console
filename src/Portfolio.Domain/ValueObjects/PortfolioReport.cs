using System.Collections.Generic;

namespace Portfolio.Domain.ValueObjects;

public class PortfolioReport
{
    public IEnumerable<ProcessedTransaction> Transactions { get; set; } = [];
    public IEnumerable<AssetHolding> Holdings { get; set; } = [];
}
