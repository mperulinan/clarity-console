import { Component, OnInit } from '@angular/core';
import { TransactionService } from '../services/transaction.service';
import { AnnotatedTransaction } from '../models/annotated-transaction';
import { Inventory } from '../models/inventory';
import { MatDialog } from '@angular/material/dialog';
import { NewTransactionV2Component } from '../new-transaction-v2/new-transaction-v2.component';
import { NewTransaction } from '../models/new-transaction';
import { ApiService } from '../services/api.service';

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
        private api: ApiService,
        private transactionService: TransactionService,
        private dialog: MatDialog,
    ) { }

    async ngOnInit(): Promise<void> {
        await this.transactionService.loadData();
        const transactions = this.transactionService.getTransactions();
        const inventory: Inventory = this.transactionService.initializeInventory(transactions);
        this.transactions = this.transactionService.getAnnotatedTransactions(transactions, inventory);

        this.isLoading = false;
    }

    openNewTransactionDialog() {
        let dialogRef = this.dialog.open(NewTransactionV2Component, {
            width: '500px',
        });

        dialogRef.afterClosed().subscribe(async (newTransaction: NewTransaction) => {
            if (!newTransaction) {
                return;
            }
            console.log("newTransaction", newTransaction);

            // this.api.postTransaction(newTransaction).then(async result => {
            //     console.log("result", result);
            // }).catch(error => {
            //     alert("Error. See console.");
            //     console.log("error", error);
            // });
        });
    }
}
