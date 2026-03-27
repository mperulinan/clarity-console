import { EnrichedAssetHolding } from './enriched-asset-holding';

export interface PortfolioMetrics {
    holdings: EnrichedAssetHolding[];
    totalPortfolioValueUsd: number;
    totalCostBasisUsd: number;
    netDepositsUsd: number;
    totalUnrealizedProfitLossUsd: number;
    totalRealizedProfitLossUsd: number;
    totalProfitLossUsd: number;
    unrealizedProfitLossPercentage: number;
    totalReturn: number;
}
