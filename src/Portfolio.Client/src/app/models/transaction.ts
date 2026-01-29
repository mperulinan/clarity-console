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
    fromAssetId: string;
    toAssetId: string;
    amountSpent: number;
    amountReceived: number;
    fromAssetPriceInEur: number;
    fee: number;
    feeAsset?: string;
    feeAssetPriceInEur?: number;
    fromAssetPriceInUsd?: number;
    usdEurExchangeRate?: number;
    notes?: string;
}
