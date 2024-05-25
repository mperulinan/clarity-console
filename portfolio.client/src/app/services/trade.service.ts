import { Injectable } from '@angular/core';
import { Trade } from '../models/trade';
import { ApiService } from './api.service';
import { CoinGeckoService } from './coin-gecko.service';

@Injectable({
    providedIn: 'root'
})
export class TradeService {

    private trades: Trade[] = [];
    assets: string[] = [];

    constructor(
        private api: ApiService,
        private coinGecko: CoinGeckoService,
    ) { }

    async loadData(): Promise<void> {
        const trades: Trade[] = await this.api.getTrades();
        this.trades = trades;
        this.updateAssets(trades);
    }

    getHoldingsByAsset(asset: string): number {
        return this.getAmountReceivedByAsset(asset) - this.getAmountSpentByAsset(asset) - this.getFeesSpentByAsset(asset);
    }

    getProfitLossInEurosByAsset(asset: string): number {
        return this.getEurosReceivedForAsset(asset) - this.getEurosSpentForAsset(asset) - this.getEurosSpentInFeesByAsset(asset) + this.getEurosOfHoldingsByAsset(asset);
    }

    private updateAssets(trades: Trade[]): void {
        const fromAssets: string[] = [...new Set(trades.map(t => t.fromAssetId))];
        const toAssets: string[] = [...new Set(trades.map(t => t.toAssetId))];
        const allAssets: string[] = [...new Set(fromAssets.concat(toAssets))];
        this.assets = allAssets;
    }

    private getTradesByFromAsset(asset: string): Trade[] {
        return this.trades.filter(t => t.fromAssetId === asset);
    }

    private getTradesByToAsset(asset: string): Trade[] {
        return this.trades.filter(t => t.toAssetId === asset);
    }

    private getTradesByFeeAsset(asset: string): Trade[] {
        return this.trades.filter(t => t.feeAsset === asset);
    }

    private getAmountSpentByAsset(asset: string): number {
        return this.getTradesByFromAsset(asset).reduce((sum, t) => sum + t.amountSpent, 0);
    }

    private getAmountReceivedByAsset(asset: string): number {
        return this.getTradesByToAsset(asset).reduce((sum, t) => sum + t.amountReceived, 0);
    }

    private getFeesSpentByAsset(asset: string): number {
        return this.getTradesByFeeAsset(asset).reduce((sum, t) => sum + t.fee, 0);
    }

    private getEurosTransacted(trades: Trade[]): number {
        let euros: number = 0;
        trades.forEach(trade => {
            euros += trade.amountSpent * trade.fromAssetPriceInEur;
        });
        return euros;
    }

    private getEurosSpentForAsset(asset: string): number {
        const trades: Trade[] = this.getTradesByToAsset(asset);
        return this.getEurosTransacted(trades);
    }

    private getEurosReceivedForAsset(asset: string): number {
        const trades: Trade[] = this.getTradesByFromAsset(asset);
        return this.getEurosTransacted(trades);
    }

    private getEurosSpentInFeesByAsset(asset: string): number {
        const trades: Trade[] = this.getTradesByFeeAsset(asset);
        let euros: number = 0;
        trades.forEach(trade => {
            if (trade.feeAssetPriceInEur) {
                euros += trade.fee * trade.feeAssetPriceInEur;
            }
        });
        return euros;
    }

    private getEurosOfHoldingsByAsset(asset: string): number {
        const price: number = this.coinGecko.prices[asset]?.eur;
        return this.getHoldingsByAsset(asset) * price;
    }
}
