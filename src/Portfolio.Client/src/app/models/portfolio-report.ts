import { AssetHolding } from './asset-holding';
import { ProcessedTransaction } from './transaction';

export interface YearSummary {
    year: number;
    totalGains: number;
    totalLosses: number;
    netPL: number;
    disallowedLosses: number;
    eventCount: number;
    errorCount: number;
}

export interface PortfolioReport {
    reportingCurrency: string;
    transactions: ProcessedTransaction[];
    holdings: AssetHolding[];
    yearSummaries: YearSummary[];
}
