import { Component, OnInit } from '@angular/core';
import { TradeService } from '../services/trade.service';
import { AnnotatedTrade } from '../models/annotated-trade';
import { Inventory } from '../models/inventory';

@Component({
    selector: 'app-transaction',
    templateUrl: './transactions.component.html',
    styleUrl: './transactions.component.scss'
})
export class TransactionsComponent implements OnInit {
    transactions: AnnotatedTrade[] = [];

    isLoading: boolean = true;
    transactionColumns: string[] = [
        'id',
        'date',
        'transactionType',
        'from',
        'to',
        'fromAssetPriceInEur',
        'fee',
        'feeAssetPriceInEur',
        // Nuevas columnas fiscales:
        'profitLoss',
        'isLossAllowed',
        'disallowedByTradeId'
    ];

    constructor(
        private tradeService: TradeService,
    ) { }

    async ngOnInit(): Promise<void> {
        await this.tradeService.loadData();
        const transactions = this.tradeService.getTrades();
        const inventory: Inventory = this.tradeService.initializeInventory(transactions);
        this.transactions = this.tradeService.getAnnotatedTrades(transactions, inventory);

        this.isLoading = false;
    }
}
