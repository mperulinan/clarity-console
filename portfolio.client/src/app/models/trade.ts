import { TransactionType } from "../services/trade.service";

export type Trade = {
    id: number,
    date: Date,
    transactionType: TransactionType,
    fromAssetId: string,
    toAssetId: string,
    amountSpent: number,
    amountReceived: number,
    fromAssetPriceInEur: number,
    fee: number,
    feeAsset: string | null,
    feeAssetPriceInEur: number | null,
    notes: string | null
};