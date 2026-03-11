export interface NewTransactionRequest {
    date: string;
    transactionTypeCode: string;
    fromAssetId?: string;
    toAssetId?: string;
    amountSpent: number;
    amountReceived: number;
    spotPriceInUsd: number;
    fee: number;
    feeAssetId?: string;
    feeSpotPriceInUsd?: number;
    notes?: string;
}
