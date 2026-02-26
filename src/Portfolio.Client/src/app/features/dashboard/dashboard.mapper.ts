import { PortfolioMetrics } from '../../models/portfolio-metrics';
import { DashboardRow } from './dashboard-row';

export function mapToDashboardRows(metrics: PortfolioMetrics): DashboardRow[] {
    if (!metrics || !metrics.holdings) {
        return [];
    }

    return metrics.holdings
        .map(h => ({
            assetId: h.id,
            name: h.name === "" ? h.id : h.name,
            symbol: h.symbol === "" ? h.id.toUpperCase() : h.symbol.toUpperCase(),
            image: h.imageUrl,
            price: h.currentPrice,
            holdingsPrice: h.currentValue,
            holdingsAmount: h.quantity,
            avgBuyPrice: h.avgCost,
            unrealizedPL: h.openPL,
            realizedPL: h.realizedPL,
            totalPL: h.totalPL,
            yieldPercentage: h.openReturn,
            allocation: h.allocationPercentage,
            totalCostBasis: h.totalCostBasis,
        }))
        .sort((a, b) => b.holdingsPrice - a.holdingsPrice);
}
