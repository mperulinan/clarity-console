import { Component, OnInit } from '@angular/core';
import { TransactionService } from '../services/transaction.service';
import { AnnotatedTransaction } from '../models/annotated-transaction';
import { Inventory } from '../models/inventory';

@Component({
    selector: 'app-transaction',
    templateUrl: './transactions.component.html',
    styleUrl: './transactions.component.scss'
})
export class TransactionsComponent implements OnInit {
    transactions: AnnotatedTransaction[] = [];

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
        'disallowedByTransactionId'
    ];

    constructor(
        private transactionService: TransactionService,
    ) { }

    async ngOnInit(): Promise<void> {
        await this.transactionService.loadData();
        const transactions = this.transactionService.getTransactions();
        const inventory: Inventory = this.transactionService.initializeInventory(transactions);
        this.transactions = this.transactionService.getAnnotatedTransactions(transactions, inventory);

        this.isLoading = false;
    }
}
