import { Injectable } from '@angular/core';
import { Transaction } from '../models/transaction';
import { ApiService } from './api.service';
import { CoinGeckoService } from './coin-gecko.service';
import { Inventory } from '../models/inventory';
import Decimal from 'decimal.js';
import { AnnotatedTransaction } from '../models/annotated-transaction';

@Injectable({
    providedIn: 'root'
})
export class TransactionService {

    private transactions: Transaction[] = [];
    assets: string[] = [];

    constructor(
        private api: ApiService,
        private coinGecko: CoinGeckoService,
    ) { }

    async loadData(): Promise<void> {
        const transactions: Transaction[] = await this.api.getTransactions();
        console.log("transactions", transactions);

        this.transactions = transactions;
        this.updateAssets(transactions);
    }

    getTransactions(): Transaction[] {
        return this.transactions;
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

    private updateAssets(transactions: Transaction[]): void {
        const fromAssets: string[] = [...new Set(transactions.map(t => t.fromAssetId))];
        const toAssets: string[] = [...new Set(transactions.map(t => t.toAssetId))];
        const allAssets: string[] = [...new Set(fromAssets.concat(toAssets))];
        this.assets = allAssets;
    }

    public getTransactionsByAsset(asset: string): Transaction[] {
        return this.transactions.filter(
            t => t.fromAssetId === asset || t.toAssetId === asset || t.feeAsset === asset
        );
    }

    private getTransactionsByFromAsset(asset: string): Transaction[] {
        return this.transactions.filter(t => t.fromAssetId === asset);
    }

    private getTransactionsByToAsset(asset: string): Transaction[] {
        return this.transactions.filter(t => t.toAssetId === asset);
    }

    private getTransactionsByFeeAsset(asset: string): Transaction[] {
        return this.transactions.filter(t => t.feeAsset === asset);
    }

    private getSwapsByFromAsset(asset: string) {
        return this.getTransactionsFilteredByTransactionType(this.getTransactionsByFromAsset(asset), this.api.appSettings.transactionType.swap);
    }

    private getSwapsByToAsset(asset: string) {
        return this.getTransactionsFilteredByTransactionType(this.getTransactionsByToAsset(asset), this.api.appSettings.transactionType.swap);
    }

    private getTransactionsFilteredByTransactionType(transactions: Transaction[], transactionTypeCode: string) {
        return transactions.filter(t => t.transactionType === transactionTypeCode);
    }

    private getTransfersInByAsset(asset: string) {
        return this.transactions.filter(t => t.toAssetId === asset && t.transactionType === this.api.appSettings.transactionType.transferIn);
    }

    private getAmountSpentByAsset(asset: string): Decimal {
        return this.getTransactionsByFromAsset(asset).reduce((sum, t) => new Decimal(sum).plus(new Decimal(t.amountSpent)), new Decimal(0));
    }

    private getAmountReceivedByAsset(asset: string): Decimal {
        return this.getTransactionsByToAsset(asset).reduce((sum, t) => new Decimal(sum).plus(new Decimal(t.amountReceived)), new Decimal(0));
    }

    private getFeesSpentByAsset(asset: string): Decimal {
        return this.getTransactionsByFeeAsset(asset).reduce((sum, t) => new Decimal(sum).plus(new Decimal(t.fee)), new Decimal(0));
    }

    private getAmountBoughtOfAsset(asset: string): Decimal {
        return this.getSwapsByToAsset(asset).reduce((sum, t) => new Decimal(sum).plus(new Decimal(t.amountReceived)), new Decimal(0));
    }

    private getEurosSpentInTransaction(transaction: Transaction): Decimal {
        return new Decimal(new Decimal(transaction.amountSpent).mul(transaction.fromAssetPriceInEur));
    }

    private getEurosSpentInTransactionWithFees(transaction: Transaction): Decimal {
        let euros: Decimal = this.getEurosSpentInTransaction(transaction);
        if (transaction.feeAssetPriceInEur) {
            euros = euros.plus(new Decimal(transaction.fee).mul(transaction.feeAssetPriceInEur));
        }
        return euros;
    }

    private getToAssetPriceInEur(transaction: Transaction): Decimal {
        if (transaction.toAssetId == transaction.fromAssetId) {
            return new Decimal(transaction.fromAssetPriceInEur);
        }
        return new Decimal(transaction.amountSpent).mul(transaction.fromAssetPriceInEur).div(transaction.amountReceived);
    }

    private getEurosReceivedInTransaction(transaction: Transaction) {
        return new Decimal(transaction.amountReceived).mul(this.getToAssetPriceInEur(transaction));
    }

    private getEurosReceivedInTransactions(transactions: Transaction[]): Decimal {
        let euros: Decimal = new Decimal(0);
        transactions.forEach(transaction => {
            euros = euros.plus(this.getEurosReceivedInTransaction(transaction));
        });
        return euros;
    }

    // Buys
    private getEurosSpentForAsset(asset: string): Decimal {
        const swaps: Transaction[] = this.getSwapsByToAsset(asset);
        let euros: Decimal = new Decimal(0);
        swaps.forEach(swap => {
            euros = euros.plus(this.getEurosSpentInTransactionWithFees(swap));
        });
        return euros;
    }

    // Sells
    private getEurosReceivedForAsset(asset: string): Decimal {
        const transactions: Transaction[] = this.getSwapsByFromAsset(asset);
        return this.getEurosReceivedInTransactions(transactions);
    }

    // Transfers In
    private getEurosTransferedInByAsset(asset: string) {
        const transfersIn: Transaction[] = this.getTransfersInByAsset(asset);
        return this.getEurosReceivedInTransactions(transfersIn);
    }

    private getEurosOfHoldingsByAsset(asset: string): Decimal {
        const price: Decimal = this.coinGecko.prices[asset]?.eur;
        if (!price) {
            return new Decimal(0);
        }
        return this.getHoldingsByAsset(asset).mul(price);
    }



    // FIFO
    sortTransactionsByDate(transactions: Transaction[]): Transaction[] {
        return transactions.sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime());
    }

    getAnnotatedTransactions(transactions: Transaction[], initialInventory: Inventory): AnnotatedTransaction[] {
        const annotatedTransactions: AnnotatedTransaction[] = this.sortTransactionsByDate(transactions).map(t => ({ ...t }));
        const fifoQueue: Inventory = { ...initialInventory };
        const lossCandidates = new Map<number, {
            transaction: AnnotatedTransaction;
            asset: string;
            date: Date;
            totalLossAmount: Decimal;
            disallowedByTransactionsId?: number;
        }>();

        for (const transaction of annotatedTransactions) {
            const { transactionType, fromAssetId, toAssetId, feeAsset } = transaction;
            const amountSpent = new Decimal(transaction.amountSpent);
            const amountReceived = new Decimal(transaction.amountReceived);
            const fromAssetPriceInEur = new Decimal(transaction.fromAssetPriceInEur);
            const toAssetPriceInEur = this.getToAssetPriceInEur(transaction);
            const fee = new Decimal(transaction.fee);
            const feeAssetPriceInEur = transaction.feeAssetPriceInEur ? new Decimal(transaction.feeAssetPriceInEur) : null;

            switch (transactionType) {
                case this.api.appSettings.transactionType.transferIn:
                    if (!fifoQueue[toAssetId]) {
                        fifoQueue[toAssetId] = [];
                    }
                    fifoQueue[toAssetId].push({ quantity: amountReceived, costInEur: toAssetPriceInEur });
                    break;

                case this.api.appSettings.transactionType.reward:
                    // REWARD genera ganancia directa.
                    let rewardProfit = amountReceived.mul(toAssetPriceInEur);

                    // Restar comisión si aplica
                    if (feeAsset && feeAssetPriceInEur) {
                        const commissionCost = fee.mul(feeAssetPriceInEur);
                        rewardProfit = rewardProfit.minus(commissionCost);

                        // Quitar comisión del inventario
                        let remainingFee = fee;
                        if (!fifoQueue[feeAsset]) {
                            throw new Error(`Not enough ${feeAsset} to cover the fee. Transaction ID ${transaction.id}`);
                        }

                        while (remainingFee.gt(0) && fifoQueue[feeAsset].length > 0) {
                            const feeEntry = fifoQueue[feeAsset][0];
                            const qty = new Decimal(feeEntry.quantity);

                            if (qty.lte(remainingFee)) {
                                remainingFee = remainingFee.minus(qty);
                                fifoQueue[feeAsset].shift();
                            } else {
                                feeEntry.quantity = qty.minus(remainingFee);
                                remainingFee = new Decimal(0);
                            }
                        }

                        if (remainingFee.gt(0)) {
                            throw new Error(`Not enough ${feeAsset} to cover the fee. Remaining fee ${remainingFee}. Transaction ID ${transaction.id}`);
                        }
                    }

                    transaction.profitLoss = rewardProfit;

                    // Se añade al inventario con el coste de ese momento en el mercado.
                    if (!fifoQueue[toAssetId]) {
                        fifoQueue[toAssetId] = [];
                    }
                    fifoQueue[toAssetId].push({ quantity: amountReceived, costInEur: toAssetPriceInEur });
                    break;

                case this.api.appSettings.transactionType.swap:
                    let transactionProfitLoss = new Decimal(0);

                    // Procesar comisión.
                    if (feeAsset && feeAssetPriceInEur) {
                        const commissionCost = fee.mul(feeAssetPriceInEur);
                        transactionProfitLoss = transactionProfitLoss.minus(commissionCost);

                        // Quitar la comisión del inventario.
                        let remainingFee = fee;
                        if (!fifoQueue[feeAsset]) {
                            throw new Error(`Not enough ${feeAsset} to cover the fee. Transaction ID ${transaction.id}`);
                        }

                        while (remainingFee.gt(0) && fifoQueue[feeAsset].length > 0) {
                            const feeEntry = fifoQueue[feeAsset][0];
                            const qty = new Decimal(feeEntry.quantity);

                            if (qty.lte(remainingFee)) {
                                remainingFee = remainingFee.minus(qty);
                                fifoQueue[feeAsset].shift();
                            } else {
                                feeEntry.quantity = qty.minus(remainingFee);
                                remainingFee = new Decimal(0);
                            }
                        }

                        if (remainingFee.gt(0)) {
                            throw new Error(`Not enough ${feeAsset} to cover the fee. Remaining fee ${remainingFee}. Transaction ID ${transaction.id}`);
                        }
                    }

                    // Venta (parte que genera ganancia/pérdida).
                    let remainingToSell = amountSpent;
                    if (!fifoQueue[fromAssetId] && remainingToSell.gt(0)) {
                        throw new Error(`Not enough ${fromAssetId} to swap. Transaction ID ${transaction.id}`);
                    }

                    while (remainingToSell.gt(0) && fifoQueue[fromAssetId].length > 0) {
                        const entry = fifoQueue[fromAssetId][0];
                        const qty = new Decimal(entry.quantity);
                        const cost = new Decimal(entry.costInEur);

                        const usedQty = Decimal.min(qty, remainingToSell);
                        const pl = usedQty.mul(fromAssetPriceInEur.minus(cost));
                        transactionProfitLoss = transactionProfitLoss.plus(pl);

                        if (qty.lte(remainingToSell)) {
                            remainingToSell = remainingToSell.minus(qty);
                            fifoQueue[fromAssetId].shift();
                        } else {
                            entry.quantity = qty.minus(remainingToSell);
                            remainingToSell = new Decimal(0);
                        }
                    }

                    if (remainingToSell.gt(0)) {
                        throw new Error(`Not enough ${fromAssetId} to swap. Remaining ${remainingToSell}. Transaction ID ${transaction.id}`);
                    }

                    // Añadir al inventario la nueva compra.
                    if (!fifoQueue[toAssetId]) {
                        fifoQueue[toAssetId] = [];
                    }
                    fifoQueue[toAssetId].push({ quantity: amountReceived, costInEur: toAssetPriceInEur });

                    // Regla fiscal de los X meses: ¿Esta compra invalida pérdidas anteriores?
                    for (const [lossId, loss] of lossCandidates.entries()) {
                        if (
                            loss.asset === toAssetId &&
                            !loss.disallowedByTransactionsId &&
                            this.isWithinXMonths(loss.date, new Date(transaction.date), 2)
                        ) {
                            loss.disallowedByTransactionsId = transaction.id;
                            loss.transaction.isLossDisallowed = true;
                            loss.transaction.disallowedByTransactionId = transaction.id;

                            if (!transaction.disallowsPreviousLosses) {
                                transaction.disallowsPreviousLosses = [];
                            }
                            transaction.disallowsPreviousLosses.push(lossId);
                        }
                    }

                    transaction.profitLoss = transactionProfitLoss;

                    // Registrar la transacción como candidata a pérdida no permitida si procede.
                    if (transactionProfitLoss.lt(0)) {
                        lossCandidates.set(transaction.id, {
                            transaction: transaction,
                            asset: fromAssetId,
                            date: new Date(transaction.date),
                            totalLossAmount: transactionProfitLoss,
                        });
                    }
                    break;

                default:
                    console.warn(`Transacción ignorada (tipo desconocido): ${transactionType} en Transaction ID ${transaction.id}`);
                    break;
            }
        }

        return annotatedTransactions;
    }

    initializeInventory(transactions: Transaction[]): Inventory {
        const fifoQueue: Inventory = {};

        transactions = this.sortTransactionsByDate(transactions);
        for (const transaction of transactions) {
            const {
                transactionType,
                fromAssetId,
                toAssetId,
                feeAsset,
            } = transaction;

            const amountReceived = new Decimal(transaction.amountReceived);
            const amountSpent = new Decimal(transaction.amountSpent);
            const fee = new Decimal(transaction.fee);
            const toAssetPriceInEur = this.getToAssetPriceInEur(transaction);
            const feeAssetPriceInEur = transaction.feeAssetPriceInEur ? new Decimal(transaction.feeAssetPriceInEur) : null;

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
                            throw new Error(`Not enough ${feeAsset} to cover fee. Transaction ID ${transaction.id}`);
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
                            throw new Error(`Not enough ${feeAsset} to cover fee. Remaining fee: ${remainingFee}. Transaction ID ${transaction.id}`);
                        }
                    }

                    // Parte de venta (consumo del inventario)
                    let remainingToSell = amountSpent;

                    if (!fifoQueue[fromAssetId]) {
                        throw new Error(`Not enough ${fromAssetId} to swap. Transaction ID ${transaction.id}`);
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
                        throw new Error(`Not enough ${fromAssetId} to swap. Remaining: ${remainingToSell}. Transaction ID ${transaction.id}`);
                    }

                    break;
                default:
                    // Otros tipos aún no implementados.
                    console.warn(`Tipo de transacción ignorado en inventario: ${transactionType} (Transaction ID ${transaction.id})`);
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

    getProfitLossForYear(transactions: AnnotatedTransaction[], year: number): Decimal {
        return transactions
            .filter(t => new Date(t.date).getFullYear() === year && t.profitLoss && !t.isLossDisallowed)
            .reduce((acc, t) => acc.plus(t.profitLoss!), new Decimal(0));
    }

    getProfitLossForAsset(transactions: AnnotatedTransaction[], assetId: string): Decimal {
        return transactions
            .filter(t =>
                (t.fromAssetId === assetId || t.toAssetId === assetId) &&
                t.profitLoss && !t.isLossDisallowed
            )
            .reduce((acc, t) => acc.plus(t.profitLoss!), new Decimal(0));
    }
}
