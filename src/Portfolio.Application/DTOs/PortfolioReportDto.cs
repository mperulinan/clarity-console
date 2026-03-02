namespace Portfolio.Application.DTOs;

public class PortfolioReportDto
{
    public IEnumerable<ProcessedTransactionDto> Transactions { get; set; } = [];
    public IEnumerable<AssetHoldingDto> Holdings { get; set; } = [];
}

public class AssetHoldingDto
{
    public Guid Id { get; set; }
    public decimal Quantity { get; set; }
    public decimal AvgCost { get; set; }
    public decimal RealizedPL { get; set; }
    public decimal CostBasisOfSold { get; set; }
}
