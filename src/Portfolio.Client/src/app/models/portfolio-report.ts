import { AssetHolding } from './asset-holding';
import { ProcessedTransaction } from './transaction';

export interface PortfolioReport {
    transactions: ProcessedTransaction[];
    holdings: AssetHolding[];
}
