export interface NewTransactionRequest {
    date: string;
    transactionTypeCode: string;
    fromAssetId: string;
    toAssetId: string;
    amountSpent: number;
    amountReceived: number;
    fromAssetPriceInUsd?: number;
    fee: number;
    feeAssetId?: string;
    feeAssetPriceInUsd?: number;
    notes?: string;
}
