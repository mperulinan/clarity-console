import Decimal from "decimal.js";
import { Transaction } from "./transaction";

export type AnnotatedTransaction = Transaction & {
    profitLoss?: Decimal;
    isLossDisallowed?: boolean;
    disallowedByTransactionId?: number;
    disallowsPreviousLosses?: number[];
};