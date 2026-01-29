import { Transaction } from './transaction';

export interface ProcessedTransaction {
    transaction: Transaction;
    profitLoss?: number;
    totalLossAmount?: number;
    isLossDisallowed?: boolean;
    disallowedByTransactionId?: number;
    disallowsPreviousLosses?: number[];
}
