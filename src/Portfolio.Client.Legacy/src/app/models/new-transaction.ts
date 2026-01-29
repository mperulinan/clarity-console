import { TransactionType } from "./transaction-type";


export interface BaseTransaction {
    date: Date | null;
    transactionType: TransactionType;
    notes: string | null;
    fee: string;
    feeAsset: string | null;
    feeAssetPriceInEur: string | null;
}

export interface SwapTransaction extends BaseTransaction {
    fromAssetId: string | null;
    toAssetId: string | null;
    amountSpent: string;
    amountReceived: string;
    fromAssetPriceInEur: string;
}

export interface RewardTransaction extends BaseTransaction {
    rewardAsset: string | null;
    rewardAmount: string;
    rewardPriceEur: string;
}

export interface TransferTransaction extends BaseTransaction {
    transferAmount: string;
}

export type NewTransaction = SwapTransaction | RewardTransaction | TransferTransaction;