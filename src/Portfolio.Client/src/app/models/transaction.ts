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
    transactionType: TransactionType;
    fromAssetId?: string;
    toAssetId?: string;
    amountSpent: number;
    amountReceived: number;
    spotPriceUSD: number;
    spotPriceEUR?: number;
    fee: number;
    feeAsset?: string;
    feePriceUSD?: number;
    feePriceEUR?: number;
    usdEurExchangeRate?: number;
    notes?: string;
}

export interface TransactionType {
    value: string;
    label: string;
    requiresFromAsset: boolean;
    requiresToAsset: boolean;
}