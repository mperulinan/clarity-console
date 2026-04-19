namespace Portfolio.Application.DTOs;

public class PortfolioReportDto
{
    public string ReportingCurrency { get; set; } = string.Empty;
    public IEnumerable<ProcessedTransactionDto> Transactions { get; set; } = [];
    public IEnumerable<AssetHoldingDto> Holdings { get; set; } = [];
    public IEnumerable<YearSummaryDto> YearSummaries { get; set; } = [];
}

public class YearSummaryDto
{
    public int Year { get; set; }
    public decimal TotalGains { get; set; }
    public decimal TotalLosses { get; set; }
    public decimal NetPL { get; set; }
    public decimal DisallowedLosses { get; set; }
    public int EventCount { get; set; }
    public int ErrorCount { get; set; }
}

public class AssetHoldingDto
{
    public Guid Id { get; set; }
    public decimal Quantity { get; set; }
    public decimal AvgCost { get; set; }
    public decimal RealizedPL { get; set; }
    public decimal CostBasisOfSold { get; set; }
}
