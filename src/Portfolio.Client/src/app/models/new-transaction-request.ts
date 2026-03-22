export interface NewTransactionRequest {
    date: string;
    transactionTypeCode: string;
    fromAssetId?: string;
    toAssetId?: string;
    amountSpent: number;
    amountReceived: number;
    spotPriceUSD?: number;
    spotPriceEUR?: number;
    spotPriceInputCurrency: string;
    fee: number;
    feeAssetId?: string;
    feePriceUSD?: number;
    feePriceEUR?: number;
    feePriceInputCurrency?: string;
    notes?: string;
}
