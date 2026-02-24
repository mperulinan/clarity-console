export interface EnrichedAssetHolding {
    assetId: string;
    imageUrl?: string;
    quantity: number;
    currentPriceUsd: number;
    currentValueUsd: number;
    avgCostUsd: number;
    totalCostBasisUsd: number;
    unrealizedProfitLossUsd: number;
    realizedProfitLossUsd: number;
    totalProfitLossUsd: number;
    yieldPercentage: number;
    allocationPercentage: number;
}
