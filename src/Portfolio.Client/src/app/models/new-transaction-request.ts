export interface NewTransactionRequest {
    date: string;
    transactionTypeCode: string;
    fromAssetId: string;
    toAssetId: string;
    amountSpent: number;
    amountReceived: number;
    fromAssetPriceInUsd?: number;
    fee: number;
    feeAsset?: string;
    feeAssetPriceInUsd?: number;
    notes?: string;
}
