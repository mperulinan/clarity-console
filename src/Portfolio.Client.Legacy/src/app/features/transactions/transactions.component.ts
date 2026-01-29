import { Component, OnInit } from '@angular/core';
import { TransactionService } from '../../core/services/transaction.service';
import { AnnotatedTransaction } from '../../core/models/annotated-transaction';
import { MatDialog } from '@angular/material/dialog';
import { NewTransactionV2Component } from './editor/new-transaction-v2.component';
import { NewTransaction } from '../../core/models/new-transaction'; 
import { ApiService } from '../../core/services/api.service';
import { CommonModule } from '@angular/common';
import { MaterialModule } from '../../material/material.module';

@Component({
    selector: 'app-transaction',
    standalone: true,
    imports: [CommonModule, MaterialModule],
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
        try {
            this.transactions = await this.transactionService.getTransactions();
        } catch (error) {
            console.error(error);
        } finally {
            this.isLoading = false;
        }
    }

    openNewTransactionDialog() {
        let dialogRef = this.dialog.open(NewTransactionV2Component, {
            width: '500px',
        });

        dialogRef.afterClosed().subscribe(async (newTransaction: NewTransaction) => {
            if (!newTransaction) {
                return;
            }
            
             this.api.postTransaction(newTransaction).then(async result => {
                 this.transactions = await this.transactionService.getTransactions(); 
             }).catch(error => {
                 alert("Error. See console.");
                 console.log("error", error);
             });
        });
    }
}
