import { EnrichedAssetHolding } from './enriched-asset-holding';

export interface PortfolioMetrics {
    holdings: EnrichedAssetHolding[];
    totalPortfolioValueUsd: number;
    totalCostBasisUsd: number;
    totalUnrealizedProfitLossUsd: number;
    totalRealizedProfitLossUsd: number;
    totalProfitLossUsd: number;
    totalProfitLossPercentage: number;
}
