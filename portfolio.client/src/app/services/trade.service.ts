import { Injectable } from '@angular/core';
import { Trade } from '../models/trade';
import { ApiService } from './api.service';
import { CoinGeckoService } from './coin-gecko.service';

export enum TransactionType {
    Swap = "SWAP",
    TransferIn = "TRANSFER_IN"
}

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
        return this.getEurosReceivedForAsset(asset) - this.getEurosSpentForAsset(asset) + this.getEurosOfHoldingsByAsset(asset) - this.getEurosTransferedInByAsset(asset);
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

    private getTradesFilteredByTransactionType(trades: Trade[], transactionType: TransactionType) {
        return trades.filter(t => t.transactionType === transactionType);
    }

    private getSwapsByFromAsset(asset: string) {
        return this.getTradesFilteredByTransactionType(this.getTradesByFromAsset(asset), TransactionType.Swap);
    }

    private getSwapsByToAsset(asset: string) {
        return this.getTradesFilteredByTransactionType(this.getTradesByToAsset(asset), TransactionType.Swap);
    }

    private getTransfersInByAsset(asset: string) {
        return this.trades.filter(t => t.toAssetId === asset && t.transactionType === TransactionType.TransferIn);
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

    private getEurosSpentInTrade(trade: Trade) {
        let euros: number = trade.amountSpent * trade.fromAssetPriceInEur;
        if (trade.feeAssetPriceInEur) {
            euros += trade.fee * trade.feeAssetPriceInEur
        }
        return euros;
    }

    private getToAssetPriceInEur(trade: Trade) {
        if (trade.toAssetId == trade.fromAssetId) {
            return trade.fromAssetPriceInEur;
        }
        return trade.amountSpent * trade.fromAssetPriceInEur / trade.amountReceived;
    }

    private getEurosReceivedInTrade(trade: Trade) {
        return trade.amountReceived * this.getToAssetPriceInEur(trade);
    }

    private getEurosReceivedInTrades(trades: Trade[]): number {
        let euros: number = 0;
        trades.forEach(trade => {
            euros += this.getEurosReceivedInTrade(trade);
        });
        return euros;
    }

    // Buys
    private getEurosSpentForAsset(asset: string): number {
        const trades: Trade[] = this.getSwapsByToAsset(asset);
        let euros: number = 0;
        trades.forEach(trade => {
            euros += this.getEurosSpentInTrade(trade);
        });
        return euros;
    }

    // Sells
    private getEurosReceivedForAsset(asset: string): number {
        const trades: Trade[] = this.getSwapsByFromAsset(asset);
        return this.getEurosReceivedInTrades(trades);
    }

    // Transfers In
    private getEurosTransferedInByAsset(asset: string) {
        const transfersIn: Trade[] = this.getTransfersInByAsset(asset);
        return this.getEurosReceivedInTrades(transfersIn);
    }

    private getEurosOfHoldingsByAsset(asset: string): number {
        const price: number = this.coinGecko.prices[asset]?.eur;
        if (!price) {
            return 0;
        }
        return this.getHoldingsByAsset(asset) * price;
    }
}
