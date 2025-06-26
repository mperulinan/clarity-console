import { Injectable } from '@angular/core';
import { Trade } from '../models/trade';
import { ApiService } from './api.service';
import { CoinGeckoService } from './coin-gecko.service';
import { Inventory } from '../models/inventory';
import Decimal from 'decimal.js';
import { AnnotatedTrade } from '../models/annotated-trade';

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

    public getTradesByAsset(asset: string): Trade[] {
        return this.trades.filter(
            t => t.fromAssetId === asset || t.toAssetId === asset || t.feeAsset === asset
        );
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

    calculateProfitsLosses(trades: Trade[], initialInventory: Inventory): {
        totalProfitLoss: Decimal;
        annotatedTrades: AnnotatedTrade[];
    } {
        const fifoQueue: Inventory = { ...initialInventory };
        let totalProfitLoss: Decimal = new Decimal(0);

        const disallowedLosses = new Map<number, {
            trade: AnnotatedTrade;
            asset: string;
            date: Date;
            remainingAmount: Decimal;
            totalLossAmount: Decimal;
            disallowedByTradeId?: number;
        }>();

        const annotatedTrades: AnnotatedTrade[] = this.sortTradesByDate(trades).map(t => ({ ...t }));
        for (const trade of annotatedTrades) {
            const {
                transactionType,
                fromAssetId,
                toAssetId,
                feeAsset
            } = trade;

            const amountSpent = new Decimal(trade.amountSpent);
            const amountReceived = new Decimal(trade.amountReceived);
            const fromAssetPriceInEur = new Decimal(trade.fromAssetPriceInEur);
            const toAssetPriceInEur = this.getToAssetPriceInEur(trade);
            const fee = new Decimal(trade.fee);
            const feeAssetPriceInEur = trade.feeAssetPriceInEur ? new Decimal(trade.feeAssetPriceInEur) : null;

            switch (transactionType) {
                case this.api.appSettings.transactionType.transferIn:
                    // Añadir al inventario sin declarar ganancia.
                    if (!fifoQueue[toAssetId]) {
                        fifoQueue[toAssetId] = [];
                    }
                    fifoQueue[toAssetId].push({ quantity: amountReceived, costInEur: toAssetPriceInEur });
                    break;

                case this.api.appSettings.transactionType.reward:
                    // Considerar directamente como ganancia.
                    const rewardGain = amountReceived.mul(toAssetPriceInEur);
                    trade.profitLoss = rewardGain;
                    totalProfitLoss = totalProfitLoss.plus(rewardGain);

                    // Se añade al inventario como coste 0 para futuras ventas.
                    if (!fifoQueue[toAssetId]) fifoQueue[toAssetId] = [];
                    fifoQueue[toAssetId].push({ quantity: amountReceived, costInEur: toAssetPriceInEur });
                    break;

                case this.api.appSettings.transactionType.swap:
                    // Comisiones.
                    if (feeAsset && feeAssetPriceInEur) {
                        totalProfitLoss = totalProfitLoss.minus(fee.mul(feeAssetPriceInEur));

                        // Quitar la comisión del inventario.
                        let remainingFeeToDeduct = fee;
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

                    // Venta.
                    let remainingToSell = amountSpent;
                    let tradeProfitLoss = new Decimal(0);

                    if (!fifoQueue[fromAssetId] && remainingToSell.gt(0)) {
                        throw new Error(`Not enough ${fromAssetId} to swap. Trade ID ${trade.id}`);
                    }

                    while (remainingToSell.gt(0) && fifoQueue[fromAssetId].length > 0) {
                        const entry = fifoQueue[fromAssetId][0];
                        const qty = new Decimal(entry.quantity);
                        const cost = new Decimal(entry.costInEur);

                        const usedQty = Decimal.min(qty, remainingToSell);
                        const pl = usedQty.mul(fromAssetPriceInEur.minus(cost));
                        tradeProfitLoss = tradeProfitLoss.plus(pl);

                        if (pl.lt(0)) {
                            disallowedLosses.set(trade.id, {
                                trade,
                                asset: fromAssetId,
                                date: new Date(trade.date),
                                remainingAmount: usedQty,
                                totalLossAmount: pl,
                            });
                        }

                        if (qty.lte(remainingToSell)) {
                            remainingToSell = remainingToSell.minus(qty);
                            fifoQueue[fromAssetId].shift();
                        } else {
                            entry.quantity = qty.minus(remainingToSell);
                            remainingToSell = new Decimal(0);
                        }
                    }

                    if (remainingToSell.gt(0)) {
                        throw new Error(`Not enough ${fromAssetId} to swap. Remaining ${remainingToSell}. Trade ID ${trade.id}`);
                    }

                    // Recompra (parte de compra del swap).
                    if (!fifoQueue[toAssetId]) {
                        fifoQueue[toAssetId] = [];
                    }
                    fifoQueue[toAssetId].push({ quantity: amountReceived, costInEur: toAssetPriceInEur });

                    // Regla X meses: esta compra invalida pérdidas pasadas.
                    for (const [lossId, loss] of disallowedLosses.entries()) {
                        if (
                            loss.asset === toAssetId &&
                            !loss.disallowedByTradeId &&
                            this.isWithinXMonths(loss.date, new Date(trade.date), 2)
                        ) {
                            loss.disallowedByTradeId = trade.id;
                            loss.trade.isLossDisallowed = true;
                            loss.trade.disallowedByTradeId = trade.id;

                            if (!trade.disallowsPreviousLosses) trade.disallowsPreviousLosses = [];
                            trade.disallowsPreviousLosses.push(lossId);
                        }
                    }

                    // Registrar ganancia/pérdida solo si es válida
                    if (!trade.isLossDisallowed && !tradeProfitLoss.isZero()) {
                        trade.profitLoss = tradeProfitLoss;
                        totalProfitLoss = totalProfitLoss.plus(tradeProfitLoss);
                    }

                    break;

                default:
                    console.warn(`Transacción ignorada (tipo desconocido): ${transactionType} en Trade ID ${trade.id}`);
                    break;
            }
        }

        return {
            totalProfitLoss,
            annotatedTrades,
        };
    }

    initializeInventory(trades: Trade[]): Inventory {
        const fifoQueue: Inventory = {};

        trades = this.sortTradesByDate(trades);
        for (const trade of trades) {
            const {
                transactionType,
                fromAssetId,
                toAssetId,
                feeAsset,
            } = trade;

            const amountReceived = new Decimal(trade.amountReceived);
            const amountSpent = new Decimal(trade.amountSpent);
            const fee = new Decimal(trade.fee);
            const toAssetPriceInEur = this.getToAssetPriceInEur(trade);
            const feeAssetPriceInEur = trade.feeAssetPriceInEur ? new Decimal(trade.feeAssetPriceInEur) : null;

            switch (transactionType) {
                case this.api.appSettings.transactionType.transferIn:
                case this.api.appSettings.transactionType.reward:
                    if (!fifoQueue[toAssetId]) {
                        fifoQueue[toAssetId] = [];
                    }
                    fifoQueue[toAssetId].push({ quantity: amountReceived, costInEur: toAssetPriceInEur });
                    break;
                case this.api.appSettings.transactionType.swap:
                    // Parte de compra
                    if (!fifoQueue[toAssetId]) {
                        fifoQueue[toAssetId] = [];
                    }
                    fifoQueue[toAssetId].push({ quantity: amountReceived, costInEur: toAssetPriceInEur });

                    // Quitar comisión
                    if (feeAsset && feeAssetPriceInEur) {
                        let remainingFee = fee;

                        if (!fifoQueue[feeAsset]) {
                            throw new Error(`Not enough ${feeAsset} to cover fee. Trade ID ${trade.id}`);
                        }

                        while (remainingFee.gt(0) && fifoQueue[feeAsset].length > 0) {
                            const entry = fifoQueue[feeAsset][0];
                            const qty = new Decimal(entry.quantity);

                            if (qty.lte(remainingFee)) {
                                remainingFee = remainingFee.minus(qty);
                                fifoQueue[feeAsset].shift();
                            } else {
                                entry.quantity = qty.minus(remainingFee);
                                remainingFee = new Decimal(0);
                            }
                        }

                        if (remainingFee.gt(0)) {
                            throw new Error(`Not enough ${feeAsset} to cover fee. Remaining fee: ${remainingFee}. Trade ID ${trade.id}`);
                        }
                    }

                    // Parte de venta (consumo del inventario)
                    let remainingToSell = amountSpent;

                    if (!fifoQueue[fromAssetId]) {
                        throw new Error(`Not enough ${fromAssetId} to swap. Trade ID ${trade.id}`);
                    }

                    while (remainingToSell.gt(0) && fifoQueue[fromAssetId].length > 0) {
                        const entry = fifoQueue[fromAssetId][0];
                        const qty = new Decimal(entry.quantity);

                        if (qty.lte(remainingToSell)) {
                            remainingToSell = remainingToSell.minus(qty);
                            fifoQueue[fromAssetId].shift();
                        } else {
                            entry.quantity = qty.minus(remainingToSell);
                            remainingToSell = new Decimal(0);
                        }
                    }

                    if (remainingToSell.gt(0)) {
                        throw new Error(`Not enough ${fromAssetId} to swap. Remaining: ${remainingToSell}. Trade ID ${trade.id}`);
                    }

                    break;
                default:
                    // Otros tipos aún no implementados.
                    console.warn(`Tipo de transacción ignorado en inventario: ${transactionType} (Trade ID ${trade.id})`);
                    break;
            }
        }

        return fifoQueue;
    }

    private isWithinXMonths(from: Date, to: Date, months: number): boolean {
        const limitDate = new Date(from);
        limitDate.setMonth(limitDate.getMonth() + months);
        return to > from && to <= limitDate;
    }
}
