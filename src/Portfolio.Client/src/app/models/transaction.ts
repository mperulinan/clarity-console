export interface ProcessedTransaction {
    transaction: Transaction;
    profitLoss?: number;
    disallowsPreviousLosses: number[];
    isLossDisallowed: boolean;
    disallowedByTransactionId?: number;
}

export interface Transaction {
    id: number;
    date: string; // ISO date string
    transactionTypeCode: string;
    fromAssetId?: string;
    toAssetId?: string;
    amountSpent: number;
    amountReceived: number;
    spotPriceInUsd: number;
    spotPriceInEur?: number;
    fee: number;
    feeAsset?: string;
    feeSpotPriceInUsd?: number;
    feeSpotPriceInEur?: number;
    usdEurExchangeRate?: number;
    notes?: string;
}
