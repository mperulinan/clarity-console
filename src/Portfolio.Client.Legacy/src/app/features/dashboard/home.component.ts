import { Component, OnInit } from '@angular/core';
import { ApiService } from '../../core/services/api.service';
import { CoinGeckoService } from '../../core/services/coin-gecko.service';
import { NewTransactionV2Component } from '../transactions/editor/new-transaction-v2.component';
import { MatDialog } from '@angular/material/dialog';
import { TransactionService } from '../../core/services/transaction.service';
import { ErApiService } from '../../core/services/er-api.service';
import { NewTransaction } from '../../core/models/new-transaction';
import { Inventory } from '../../core/models/inventory';
import { Coin } from '../../core/models/coin';
import { AnnotatedTransaction } from '../../core/models/annotated-transaction';
import { CommonModule } from '@angular/common';
import { MaterialModule } from '../../material/material.module';

@Component({
    selector: 'app-home',
    standalone: true,
    imports: [CommonModule, MaterialModule],
    templateUrl: './home.component.html',
    styleUrl: './home.component.scss'
})
export class HomeComponent implements OnInit {
    public totalValueInEur: number = 0;
    public totalCost: number = 0;
    public profitLoss: number = 0;

    transactions: AnnotatedTransaction[] = [];
    inventory: any = {}; 

    portfolio: { asset: string, quantity: number, price: number, value: number, cost: number, pl: number, plPercentage: number }[] = [];

    // Charts
    public chartOptions: any;
    public chartOptionsPie: any;

    constructor(
        private api: ApiService,
        private coinGecko: CoinGeckoService,
        private dialog: MatDialog,
        private transactionService: TransactionService,
        private erApi: ErApiService
    ) { }

    async ngOnInit() {
        try {
            this.transactions = await this.transactionService.getTransactions();
            const coins = await this.coinGecko.getAllCoins();
            // TODO: Implement portfolio calculation
        } catch (e) {
            console.error(e);
        }
    }

    openNewTransactionDialog() {
        let dialogRef = this.dialog.open(NewTransactionV2Component, {
            width: '500px',
        });

        dialogRef.afterClosed().subscribe(async (newTransaction: NewTransaction) => {
            if (!newTransaction) return;
            
            this.api.postTransaction(newTransaction).then(async result => {
                 this.transactions = await this.transactionService.getTransactions(); 
            }).catch(error => {
                console.log("error", error);
            });
        });
    }
}
