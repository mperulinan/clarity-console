export interface NewTransactionRequest {
    date: string;
    transactionTypeCode: string;
    fromAssetId?: string;
    toAssetId?: string;
    amountSpent: number;
    amountReceived: number;
    spotPriceInUsd?: number;
    spotPriceInEur?: number;
    spotPriceInputCurrency: string;
    fee: number;
    feeAssetId?: string;
    feeSpotPriceInUsd?: number;
    feeSpotPriceInEur?: number;
    feePriceInputCurrency?: string;
    notes?: string;
}
