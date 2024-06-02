import { Injectable } from '@angular/core';
import { Trade } from '../models/trade';
import { ApiService } from './api.service';
import { CoinGeckoService } from './coin-gecko.service';
import { Inventory } from '../models/inventory';
import Decimal from 'decimal.js';

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
        console.log("trades", trades);

        this.trades = trades;
        this.updateAssets(trades);
    }

    getHoldingsByAsset(asset: string): Decimal {
        if (asset == 'monsta-infinite') {
            console.log("in");
        }
        const amountReceived = this.getAmountReceivedByAsset(asset);
        const amountSpent = this.getAmountSpentByAsset(asset);
        const feesSpent = this.getFeesSpentByAsset(asset);
        return amountReceived.minus(amountSpent).minus(feesSpent);
    }

    getProfitLossInEurosByAsset(asset: string): Decimal {
        return this.getEurosReceivedForAsset(asset).minus(this.getEurosSpentForAsset(asset)).plus(this.getEurosOfHoldingsByAsset(asset)).minus(this.getEurosTransferedInByAsset(asset));
    }

    getProfitLossPercentageByAsset(asset: string): Decimal {
        const eurosSpent = this.getEurosSpentForAsset(asset).plus(this.getEurosTransferedInByAsset(asset));
        const profitLoss = this.getProfitLossInEurosByAsset(asset);

        if (eurosSpent.eq(0)) {
            return profitLoss.gt(0) ? new Decimal(Infinity) : new Decimal(-100);
        }

        return (profitLoss.div(eurosSpent)).mul(100);
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

    private getAmountSpentByAsset(asset: string): Decimal {
        return this.getTradesByFromAsset(asset).reduce((sum, t) => new Decimal(sum).plus(new Decimal(t.amountSpent)), new Decimal(0));
    }

    private getAmountReceivedByAsset(asset: string): Decimal {
        return this.getTradesByToAsset(asset).reduce((sum, t) => new Decimal(sum).plus(new Decimal(t.amountReceived)), new Decimal(0));
    }

    private getFeesSpentByAsset(asset: string): Decimal {
        return this.getTradesByFeeAsset(asset).reduce((sum, t) => new Decimal(sum).plus(new Decimal(t.fee)), new Decimal(0));
    }

    private getEurosSpentInTrade(trade: Trade): Decimal {
        let euros: Decimal = new Decimal(new Decimal(trade.amountSpent).mul(trade.fromAssetPriceInEur));
        if (trade.feeAssetPriceInEur) {
            euros = euros.plus(new Decimal(trade.fee).mul(trade.feeAssetPriceInEur));
        }
        return euros;
    }

    private getToAssetPriceInEur(trade: Trade): Decimal {
        if (trade.toAssetId == trade.fromAssetId) {
            return new Decimal(trade.fromAssetPriceInEur);
        }
        return new Decimal(trade.amountSpent).mul(trade.fromAssetPriceInEur).div(trade.amountReceived);
    }

    private getEurosReceivedInTrade(trade: Trade) {
        return new Decimal(trade.amountReceived).mul(this.getToAssetPriceInEur(trade));
    }

    private getEurosReceivedInTrades(trades: Trade[]): Decimal {
        let euros: Decimal = new Decimal(0);
        trades.forEach(trade => {
            euros = euros.plus(this.getEurosReceivedInTrade(trade));
        });
        return euros;
    }

    // Buys
    private getEurosSpentForAsset(asset: string): Decimal {
        const trades: Trade[] = this.getSwapsByToAsset(asset);
        let euros: Decimal = new Decimal(0);
        trades.forEach(trade => {
            euros = euros.plus(this.getEurosSpentInTrade(trade));
        });
        return euros;
    }

    // Sells
    private getEurosReceivedForAsset(asset: string): Decimal {
        const trades: Trade[] = this.getSwapsByFromAsset(asset);
        return this.getEurosReceivedInTrades(trades);
    }

    // Transfers In
    private getEurosTransferedInByAsset(asset: string) {
        const transfersIn: Trade[] = this.getTransfersInByAsset(asset);
        return this.getEurosReceivedInTrades(transfersIn);
    }

    private getEurosOfHoldingsByAsset(asset: string): Decimal {
        const price: number = this.coinGecko.prices[asset]?.eur;
        if (!price) {
            return new Decimal(0);
        }
        return this.getHoldingsByAsset(asset).mul(price.toString());
    }



    // FIFO
    sortTradesByDate(trades: Trade[]): Trade[] {
        return trades.sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime());
    }

    getTradesBeforeYear(year: number): Trade[] {
        return this.trades.filter(trade => new Date(trade.date).getFullYear() < year);
    }

    getTradesByYear(year: number): Trade[] {
        return this.trades.filter(trade => new Date(trade.date).getFullYear() === year);
    }

    calculateProfitsLosses(trades: Trade[], initialInventory: Inventory): Decimal {
        const fifoQueue: Inventory = { ...initialInventory };
        let totalProfitLoss: Decimal = new Decimal(0);

        trades = this.sortTradesByDate(trades);
        for (const trade of trades) {
            const {
                transactionType, fromAssetId, toAssetId,
                feeAsset
            } = trade;
            const amountSpent = new Decimal(trade.amountSpent);
            const amountReceived = new Decimal(trade.amountReceived);
            const fromAssetPriceInEur = new Decimal(trade.fromAssetPriceInEur);
            const toAssetPriceInEur = this.getToAssetPriceInEur(trade);
            const fee = new Decimal(trade.fee);
            const feeAssetPriceInEur = trade.feeAssetPriceInEur ? new Decimal(trade.feeAssetPriceInEur) : null;

            // Gestionar las comisiones
            if (feeAsset && feeAssetPriceInEur) {
                totalProfitLoss = totalProfitLoss.minus(fee.mul(feeAssetPriceInEur));
            }

            if (transactionType === TransactionType.TransferIn) {
                // Añadir la cantidad recibida al FIFO del activo destino (toAssetId)
                if (!fifoQueue[toAssetId]) {
                    fifoQueue[toAssetId] = [];
                }
                fifoQueue[toAssetId].push({ quantity: amountReceived, costInEur: toAssetPriceInEur });
            } else if (transactionType === TransactionType.Swap) {
                // Procesar la parte de "vender" del swap
                let remainingQuantityToSell: Decimal = amountSpent;

                if (!fifoQueue[fromAssetId] && remainingQuantityToSell.gt(0)) {
                    throw new Error(`Not enough ${fromAssetId} to swap. Trade ID ${trade.id}`);
                }

                while (remainingQuantityToSell.gt(0) && fifoQueue[fromAssetId].length > 0) {
                    const firstEntry = fifoQueue[fromAssetId][0];
                    const firstEntryQuantity: Decimal = new Decimal(firstEntry.quantity);

                    if (firstEntryQuantity.lte(remainingQuantityToSell)) {
                        totalProfitLoss = totalProfitLoss.plus(firstEntryQuantity.mul((fromAssetPriceInEur.minus(firstEntry.costInEur))));
                        remainingQuantityToSell = remainingQuantityToSell.minus(firstEntryQuantity);
                        fifoQueue[fromAssetId].shift();
                    } else {
                        totalProfitLoss = totalProfitLoss.plus(remainingQuantityToSell.mul((fromAssetPriceInEur.minus(firstEntry.costInEur))));
                        firstEntry.quantity = firstEntryQuantity.minus(remainingQuantityToSell);
                        remainingQuantityToSell = new Decimal(0);
                    }
                }

                if (remainingQuantityToSell.gt(0)) {
                    throw new Error(`Not enough ${fromAssetId} to swap. Remaining ${remainingQuantityToSell}. Trade ID ${trade.id}`);
                }

                // Procesar la parte de "comprar" del swap
                if (!fifoQueue[toAssetId]) {
                    fifoQueue[toAssetId] = [];
                }
                fifoQueue[toAssetId].push({ quantity: amountReceived, costInEur: toAssetPriceInEur });
            }
        }

        return totalProfitLoss;
    }

    initializeInventory(trades: Trade[]): Inventory {
        const fifoQueue: Inventory = {};

        trades = this.sortTradesByDate(trades);
        for (const trade of trades) {
            const {
                transactionType, fromAssetId, toAssetId,
            } = trade;

            if (trade.id == 312) {
                console.log("in");
            }

            if (!fifoQueue[toAssetId]) {
                fifoQueue[toAssetId] = [];
            }
            fifoQueue[toAssetId].push({ quantity: new Decimal(trade.amountReceived), costInEur: this.getToAssetPriceInEur(trade) });

            if (transactionType === TransactionType.Swap) {
                let remainingQuantityToSell: Decimal = new Decimal(trade.amountSpent);

                if (!fifoQueue[fromAssetId]) {
                    throw new Error(`Not enough ${fromAssetId} to swap. Trade ID ${trade.id}`);
                }

                while (remainingQuantityToSell.gt(0) && fifoQueue[fromAssetId].length > 0) {
                    const firstEntry = fifoQueue[fromAssetId][0];

                    if (firstEntry.quantity.lte(remainingQuantityToSell)) {
                        remainingQuantityToSell = remainingQuantityToSell.minus(firstEntry.quantity);
                        fifoQueue[fromAssetId].shift();
                    } else {
                        firstEntry.quantity = firstEntry.quantity.minus(remainingQuantityToSell);
                        remainingQuantityToSell = new Decimal(0);
                    }
                }

                if (remainingQuantityToSell.gt(0)) {
                    throw new Error(`Not enough ${fromAssetId} to swap. Remaining ${remainingQuantityToSell}. Trade ID ${trade.id}`);
                }
            }
        }

        return fifoQueue;
    }
}
