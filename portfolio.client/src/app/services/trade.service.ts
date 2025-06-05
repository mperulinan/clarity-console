import { Injectable } from '@angular/core';
import { Trade } from '../models/trade';
import { ApiService } from './api.service';
import { CoinGeckoService } from './coin-gecko.service';
import { Inventory } from '../models/inventory';
import Decimal from 'decimal.js';

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
        const amountReceived = this.getAmountReceivedByAsset(asset);
        const amountSpent = this.getAmountSpentByAsset(asset);
        const feesSpent = this.getFeesSpentByAsset(asset);
        return amountReceived.minus(amountSpent).minus(feesSpent);
    }

    getAvgBuyPriceByAsset(asset: string): Decimal {
        const amountBought = this.getAmountBoughtOfAsset(asset);
        const cost = this.getEurosSpentForAsset(asset);
        return cost.div(amountBought);
    }

    getProfitLossInEurosByAsset(asset: string): Decimal {
        const eurosReceived = this.getEurosReceivedForAsset(asset);
        const eurosSpent = this.getEurosSpentForAsset(asset);
        const eurosOfHoldings = this.getEurosOfHoldingsByAsset(asset);
        const eurosTransferedIn = this.getEurosTransferedInByAsset(asset);

        return eurosReceived.minus(eurosSpent).plus(eurosOfHoldings).minus(eurosTransferedIn);
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

    private getSwapsByFromAsset(asset: string) {
        return this.getTradesFilteredByTransactionType(this.getTradesByFromAsset(asset), this.api.appSettings.transactionType.swap);
    }

    private getSwapsByToAsset(asset: string) {
        return this.getTradesFilteredByTransactionType(this.getTradesByToAsset(asset), this.api.appSettings.transactionType.swap);
    }

    private getTradesFilteredByTransactionType(trades: Trade[], transactionTypeCode: string) {
        return trades.filter(t => t.transactionType === transactionTypeCode);
    }

    private getTransfersInByAsset(asset: string) {
        return this.trades.filter(t => t.toAssetId === asset && t.transactionType === this.api.appSettings.transactionType.transferIn);
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

    private getAmountBoughtOfAsset(asset: string): Decimal {
        return this.getSwapsByToAsset(asset).reduce((sum, t) => new Decimal(sum).plus(new Decimal(t.amountReceived)), new Decimal(0));
    }

    private getEurosSpentInTrade(trade: Trade): Decimal {
        return new Decimal(new Decimal(trade.amountSpent).mul(trade.fromAssetPriceInEur));
    }

    private getEurosSpentInTradeWithFees(trade: Trade): Decimal {
        let euros: Decimal = this.getEurosSpentInTrade(trade);
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
        const swaps: Trade[] = this.getSwapsByToAsset(asset);
        let euros: Decimal = new Decimal(0);
        swaps.forEach(swap => {
            euros = euros.plus(this.getEurosSpentInTradeWithFees(swap));
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
        const price: Decimal = this.coinGecko.prices[asset]?.eur;
        if (!price) {
            return new Decimal(0);
        }
        return this.getHoldingsByAsset(asset).mul(price);
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
                transactionType: transactionType, fromAssetId, toAssetId,
                feeAsset
            } = trade;
            const amountSpent = new Decimal(trade.amountSpent);
            const amountReceived = new Decimal(trade.amountReceived);
            const fromAssetPriceInEur = new Decimal(trade.fromAssetPriceInEur);
            const toAssetPriceInEur = this.getToAssetPriceInEur(trade);
            const fee = new Decimal(trade.fee);
            const feeAssetPriceInEur = trade.feeAssetPriceInEur ? new Decimal(trade.feeAssetPriceInEur) : null;

            if (transactionType === this.api.appSettings.transactionType.transferIn) {
                // Añadir la cantidad recibida al FIFO del activo destino (toAssetId)
                if (!fifoQueue[toAssetId]) {
                    fifoQueue[toAssetId] = [];
                }
                fifoQueue[toAssetId].push({ quantity: amountReceived, costInEur: toAssetPriceInEur });
            } else if (transactionType === this.api.appSettings.transactionType.swap) {
                if (feeAsset && feeAssetPriceInEur) {
                    totalProfitLoss = totalProfitLoss.minus(fee.mul(feeAssetPriceInEur));

                    // Deducir la comisión del inventario
                    let remainingFeeToDeduct: Decimal = fee;
                    if (!fifoQueue[feeAsset]) {
                        throw new Error(`Not enough ${feeAsset} to cover the fee. Trade ID ${trade.id}`);
                    }

                    while (remainingFeeToDeduct.gt(0) && fifoQueue[feeAsset].length > 0) {
                        const firstFeeEntry = fifoQueue[feeAsset][0];
                        const firstFeeEntryQuantity: Decimal = new Decimal(firstFeeEntry.quantity);

                        if (firstFeeEntryQuantity.lte(remainingFeeToDeduct)) {
                            remainingFeeToDeduct = remainingFeeToDeduct.minus(firstFeeEntryQuantity);
                            fifoQueue[feeAsset].shift();
                        } else {
                            firstFeeEntry.quantity = firstFeeEntryQuantity.minus(remainingFeeToDeduct);
                            remainingFeeToDeduct = new Decimal(0);
                        }
                    }

                    if (remainingFeeToDeduct.gt(0)) {
                        throw new Error(`Not enough ${feeAsset} to cover the fee. Remaining fee ${remainingFeeToDeduct}. Trade ID ${trade.id}`);
                    }
                }

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
                transactionType: transactionType, fromAssetId, toAssetId, feeAsset,
            } = trade;
            const fee = new Decimal(trade.fee);
            const feeAssetPriceInEur = trade.feeAssetPriceInEur ? new Decimal(trade.feeAssetPriceInEur) : null;

            if (!fifoQueue[toAssetId]) {
                fifoQueue[toAssetId] = [];
            }
            fifoQueue[toAssetId].push({ quantity: new Decimal(trade.amountReceived), costInEur: this.getToAssetPriceInEur(trade) });

            if (transactionType === this.api.appSettings.transactionType.swap) {
                // Reducir las comisiones
                if (feeAsset && feeAssetPriceInEur) {
                    let remainingFeeToDeduct: Decimal = fee;

                    if (!fifoQueue[feeAsset]) {
                        throw new Error(`Not enough ${feeAsset} to cover the fee. Trade ID ${trade.id}`);
                    }

                    while (remainingFeeToDeduct.gt(0) && fifoQueue[feeAsset].length > 0) {
                        const firstFeeEntry = fifoQueue[feeAsset][0];
                        const firstFeeEntryQuantity: Decimal = new Decimal(firstFeeEntry.quantity);

                        if (firstFeeEntryQuantity.lte(remainingFeeToDeduct)) {
                            remainingFeeToDeduct = remainingFeeToDeduct.minus(firstFeeEntryQuantity);
                            fifoQueue[feeAsset].shift();
                        } else {
                            firstFeeEntry.quantity = firstFeeEntryQuantity.minus(remainingFeeToDeduct);
                            remainingFeeToDeduct = new Decimal(0);
                        }
                    }

                    if (remainingFeeToDeduct.gt(0)) {
                        throw new Error(`Not enough ${feeAsset} to cover the fee. Remaining fee ${remainingFeeToDeduct}. Trade ID ${trade.id}`);
                    }
                }

                // Gestionar los activos intercambiados
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
