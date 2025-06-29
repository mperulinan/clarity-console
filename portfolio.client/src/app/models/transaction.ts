export type Transaction = {
    id: number,
    date: Date,
    transactionType: string,
    fromAssetId: string,
    toAssetId: string,
    amountSpent: string,
    amountReceived: string,
    fromAssetPriceInEur: string,
    fee: string,
    feeAsset: string | null,
    feeAssetPriceInEur: string | null,
    notes: string | null
};