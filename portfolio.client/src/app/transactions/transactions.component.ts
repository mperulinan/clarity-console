import { Component, OnInit } from '@angular/core';
import { TradeService } from '../services/trade.service';
import { ActivatedRoute } from '@angular/router';
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
        private route: ActivatedRoute
    ) { }

    async ngOnInit(): Promise<void> {
        this.route.paramMap.subscribe(async params => {

        });
        await this.tradeService.loadData();
        const transacions = this.tradeService.getTradesBeforeYear(2026);
        const inventory: Inventory = this.tradeService.initializeInventory(transacions);
        this.transactions = this.tradeService.calculateProfitsLosses(transacions, inventory).annotatedTrades;

        this.isLoading = false;
    }
}
