import { Transaction } from "./transaction";

export type AnnotatedTransaction = Transaction & {
    profitLoss?: number;
    totalLossAmount?: number;
    isLossDisallowed?: boolean;
    disallowedByTransactionId?: number;
    disallowsPreviousLosses?: number[];
};
