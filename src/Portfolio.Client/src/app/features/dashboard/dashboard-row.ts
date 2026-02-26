export interface DashboardRow {
    assetId: string;
    name: string;
    symbol: string;
    image?: string;
    price: number;
    holdingsPrice: number;
    holdingsAmount: number;
    avgBuyPrice: number;
    unrealizedPL: number;
    realizedPL: number;
    totalPL: number;
    yieldPercentage: number;
    allocation: number;
    totalCostBasis: number;
}
