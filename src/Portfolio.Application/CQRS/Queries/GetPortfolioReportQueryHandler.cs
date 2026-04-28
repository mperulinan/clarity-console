using MediatR;
using Microsoft.Extensions.Logging;
using Portfolio.Application.DTOs;
using Portfolio.Application.Mappers;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;
using Portfolio.Domain.Services;

namespace Portfolio.Application.CQRS.Queries;

public class GetPortfolioReportQueryHandler(
    IUnitOfWork unitOfWork,
    IInventoryCalculator inventoryCalculator,
    ILogger<GetPortfolioReportQueryHandler> logger) 
    : IRequestHandler<GetPortfolioReportQuery, PortfolioReportDto>
{
    private static readonly FiatCurrency FinancialReportingCurrency = FiatCurrency.EUR;

    public async Task<PortfolioReportDto> Handle(GetPortfolioReportQuery query, CancellationToken cancellationToken)
    {
        logger.LogInformation("Generating portfolio report ({Currency})...", FinancialReportingCurrency.Value);

        var transactions = await unitOfWork.Transactions.GetAllAsync();
        var report = inventoryCalculator.CalculateInventory(transactions, FinancialReportingCurrency);
        var processedList = report.Transactions.ToList();

        var yearSummaries = processedList
            .GroupBy(pt => pt.Transaction.Date.Year)
            .Select(g =>
            {
                decimal totalGains = 0, totalLosses = 0, disallowed = 0;
                int errorCount = 0;

                foreach (var pt in g)
                {
                    if (pt.ProfitLoss.HasValue)
                    {
                        if (pt.IsLossDisallowed)
                        {
                            disallowed += Math.Abs(pt.ProfitLoss.Value);
                        }
                        else if (pt.ProfitLoss.Value >= 0)
                        {
                            totalGains += pt.ProfitLoss.Value;
                        }
                        else
                        {
                            totalLosses += pt.ProfitLoss.Value;
                        }
                    }

                    if (pt.Error != null) errorCount++;
                }

                return new YearSummaryDto
                {
                    Year = g.Key,
                    TotalGains = totalGains,
                    TotalLosses = totalLosses,
                    NetPL = totalGains + totalLosses,
                    DisallowedLosses = disallowed,
                    EventCount = g.Count(),
                    ErrorCount = errorCount
                };
            })
            .OrderByDescending(ys => ys.Year)
            .ToList();

        var dto = new PortfolioReportDto
        {
            ReportingCurrency = FinancialReportingCurrency.Value.ToUpperInvariant(),
            Transactions = processedList.Select(pt => pt.ToDto()),
            Holdings = report.Holdings.Select(h => new AssetHoldingDto
            {
                Id = h.Id,
                Quantity = h.Quantity,
                AvgCost = h.AvgCost,
                RealizedPL = h.RealizedPL,
                CostBasisOfSold = h.CostBasisOfSold
            }),
            YearSummaries = yearSummaries
        };

        logger.LogInformation("Portfolio report generated with {TransactionCount} processed transactions.",
            processedList.Count);

        return dto;
    }
}
