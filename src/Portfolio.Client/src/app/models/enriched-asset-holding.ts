export interface EnrichedAssetHolding {
    id: string;
    symbol: string;
    name: string;
    imageUrl?: string;
    quantity: number;
    currentPrice: number;
    currentValue: number;
    avgCost: number;
    totalCostBasis: number;
    costBasisOfSold: number;
    openPL: number;
    openReturn: number;
    realizedPL: number;
    realizedReturn: number;
    totalPL: number;
    totalReturn: number;
    allocationPercentage: number;
}
