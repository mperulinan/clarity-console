import Decimal from "decimal.js";
import { Trade } from "./trade";

export type AnnotatedTrade = Trade & {
    profitLoss?: Decimal;
    isLossDisallowed?: boolean;
    disallowedByTradeId?: number;
    disallowsPreviousLosses?: number[];
};