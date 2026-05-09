import { AssetDto } from '../models/asset';

export interface ProcessedTransaction {
    transaction: Transaction;
    profitLoss?: number;
    totalLossAmount?: number;
    disallowsPreviousLosses: number[];
    isLossDisallowed: boolean;
    disallowedByTransactionId?: number;
    error?: string;
}

export interface Transaction {
    id: number;
    date: string; // ISO date string
    type: TransactionType;
    fromAsset?: AssetDto;
    toAsset?: AssetDto;
    amountSpent: number;
    amountReceived: number;
    spotPriceUSD?: number;
    spotPriceEUR?: number;
    fee: number;
    feeAsset?: AssetDto;
    feePriceUSD?: number;
    feePriceEUR?: number;
    usdEurExchangeRate?: number;
    notes?: string;
}

export interface TransactionType {
    value: string;
    name: string;
    requiresFromAsset: boolean;
    requiresToAsset: boolean;
}