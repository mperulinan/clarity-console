import { TransactionType } from "./transaction-type";

export type NewTransaction = {
    date: Date;
    transactionType: string;
    notes: string;
    
    // Swap/General props
    fromAssetId: string;
    toAssetId: string;
    amountSpent: number;
    amountReceived: number;
    
    fee: number;
    feeAsset: string;
    
    fromAssetPriceInEur: number;
    feeAssetPriceInEur: number;

    // Optional legacy fields if ever needed
    rewardAsset?: string;
    rewardAmount?: number;
    rewardPriceEur?: number;
    transferAmount?: number;
};
