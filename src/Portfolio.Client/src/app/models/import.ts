export interface BrokerDto {
  key: string;
  displayName: string;
}

export interface ImportedTransactionRow {
  date: string;
  productName: string;
  isin: string;
  exchange: string;
  quantity: number;
  pricePerUnit: number;
  priceCurrency: string;
  fxRate?: number | null;
  feeEur?: number | null;
  totalEur: number;
  brokerTransactionId: string;
}

export interface ImportedTransactionRowPreview {
  raw: ImportedTransactionRow;
  resolvedSymbol?: string | null;
  resolvedAssetId?: string | null;
  willAutoCreate: boolean;
  isDuplicate: boolean;
  isUnresolvable: boolean;
  warningMessage?: string | null;
}

export interface ImportTransactionsPreviewDto {
  previewToken: string;
  rows: ImportedTransactionRowPreview[];
  totalRows: number;
  duplicateRows: number;
  unresolvableRows: number;
  readyToImportRows: number;
}

export interface CommitImportTransactionsRequest {
  previewToken: string;
  excludedBrokerTransactionIds?: string[];
  overriddenSymbols?: Record<string, string>;
}

export interface ImportTransactionsResultDto {
  importedCount: number;
  skippedCount: number;
  warnings: string[];
}

export type ImportWizardStep = 'upload' | 'review' | 'result';

export interface ReviewRowItem extends ImportedTransactionRowPreview {
  selected: boolean;
}
