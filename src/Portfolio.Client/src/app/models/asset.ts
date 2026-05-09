export interface AssetDto {
    id: string; // The GUID from the DB, or empty if unsynced
    symbol: string;
    name: string;
    externalId?: string;
    imageUrl?: string;
    type: string;
    transactionCount: number;
    marketCapRank?: number;
}
