import { TransactionType } from "../services/trade.service";

export type Trade = {
    id: number,
    date: Date,
    transactionType: TransactionType,
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