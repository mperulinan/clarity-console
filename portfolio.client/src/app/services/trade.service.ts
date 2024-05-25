import { Injectable } from '@angular/core';
import { Trade } from '../models/trade';
import { ApiService } from './api.service';

@Injectable({
    providedIn: 'root'
})
export class TradeService {

    private trades: Trade[] = [];
    assets: string[] = [];

    constructor(
        private api: ApiService
    ) { }

    async loadData(): Promise<void> {
        const trades: Trade[] = await this.api.getTrades();
        this.trades = trades;
        this.updateAssets(trades);
    }

    getHoldingsByAsset(asset: string): number {
        return this.getAmountReceivedByAsset(asset) - this.getAmountSpentByAsset(asset) - this.getFeesSpentByAsset(asset);
    }

    private updateAssets(trades: Trade[]): void {
        const fromAssets: string[] = [...new Set(trades.map(t => t.fromAssetId))];
        const toAssets: string[] = [...new Set(trades.map(t => t.toAssetId))];
        const allAssets: string[] = [...new Set(fromAssets.concat(toAssets))];
        this.assets = allAssets;
    }

    private getAmountSpentByAsset(asset: string): number {
        return this.trades.filter(t => t.fromAssetId === asset).reduce((sum, t) => sum + t.amountSpent, 0);
    }

    private getAmountReceivedByAsset(asset: string): number {
        return this.trades.filter(t => t.toAssetId === asset).reduce((sum, t) => sum + t.amountReceived, 0);
    }

    private getFeesSpentByAsset(asset: string): number {
        return this.trades.filter(t => t.feeAsset === asset).reduce((sum, t) => sum + t.fee, 0);
    }
}
