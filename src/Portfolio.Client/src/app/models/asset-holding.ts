export interface AssetHolding {
    assetId: string;
    quantity: number;
    avgCostInEur: number;
    realizedProfitLossEur: number;
    currentPriceInEur?: number;
    currentValueInEur?: number;
}
