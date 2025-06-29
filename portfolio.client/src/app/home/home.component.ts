import { Component } from '@angular/core';
import Decimal from 'decimal.js';
import { ApiService } from '../services/api.service';
import { CoinGeckoService } from '../services/coin-gecko.service';
import { MatDialog } from '@angular/material/dialog';
import { TransactionService } from '../services/transaction.service';
import { ErApiService } from '../services/er-api.service';
import { NewTransactionComponent } from '../new-transaction/new-transaction.component';
import { NewTransaction } from '../models/new-transaction';
import { Inventory } from '../models/inventory';
import { Coin } from '../models/coin';

export type PortfolioRow = {
    image: string,
    name: string,
    symbol: string,
    price: Decimal,
    holdingsPrice: Decimal,
    holdingsAmount: Decimal,
    avgBuyPrice: Decimal,
    profitLoss: Decimal,
    profitLossPercentage: Decimal,
};

@Component({
    selector: 'app-home',
    templateUrl: './home.component.html',
    styleUrl: './home.component.scss'
})
export class HomeComponent {
    displayedColumns: string[] = ['assetId', 'price', 'holdings', 'avgBuyPrice', 'profitLoss', 'percentage'];
    portfolioRows: PortfolioRow[] = [];
    portfolioValue: Decimal = new Decimal(0);
    profitLoss: Decimal = new Decimal(0);
    profitLossPercentage: Decimal = new Decimal(0);
    profitLossPerYear: { [key: number]: Decimal } = {};
    currencySymbol: string = "$";
    eurToUsd: Decimal = new Decimal(0);

    // Inteface control
    isLoading: boolean = true;

    constructor(
        private api: ApiService,
        private coinGecko: CoinGeckoService,
        private dialog: MatDialog,
        private transactionService: TransactionService,
        private erApi: ErApiService,
    ) { }

    async ngOnInit() {
        await this.transactionService.loadData();
        await this.coinGecko.loadData(this.transactionService.assets);

        await this.loadTable();
        this.setPortfolioValue();
        this.setProfitLoss();
        this.setProfitLossPercentage();
        this.setProfitLossPerYear();
        this.eurToUsd = await this.erApi.getEur2Usd();
    }

    async loadTable() {
        const assets: string[] = this.transactionService.assets;

        for (let index = 0; index < assets.length; index++) {
            const asset = assets[index];
            const holdings = this.transactionService.getHoldingsByAsset(asset);

            let coin: Coin | undefined = this.coinGecko.coins.find(c => c.id == asset);

            let price: any = this.coinGecko.prices[asset]?.usd;
            price = price ? price : 0;

            let holdingsPrice: Decimal = holdings.mul(price);
            let profitLoss: Decimal = this.transactionService.getProfitLossInEurosByAsset(asset);
            let newRow: PortfolioRow = {
                image: coin?.image ?? "",
                name: coin?.name ?? asset,
                symbol: coin?.symbol.toUpperCase() ?? '',
                price: price,
                holdingsPrice: holdingsPrice,
                holdingsAmount: holdings,
                avgBuyPrice: this.transactionService.getAvgBuyPriceByAsset(asset),
                profitLoss: profitLoss,
                profitLossPercentage: this.transactionService.getProfitLossPercentageByAsset(asset),
            };

            this.portfolioRows.push(newRow);
        }

        this.portfolioRows.sort((a, b) => b.holdingsPrice.comparedTo(a.holdingsPrice));
        console.log("portfolioRows", this.portfolioRows);


        this.isLoading = false;
    }

    openNewTransactionDialog() {
        let dialogRef = this.dialog.open(NewTransactionComponent, {
            width: '500px',
            data: { coins: this.coinGecko.coins },
        });

        dialogRef.afterClosed().subscribe(async (newTransaction: NewTransaction) => {
            if (!newTransaction) {
                return;
            }
            console.log("newTransaction", newTransaction);

            this.api.postTransaction(newTransaction).then(async result => {
                await this.loadTable();
            }).catch(error => {
                alert("Error. See console.");
                console.log("error", error);
            });
        });
    }

    private setPortfolioValue() {
        let value: Decimal = new Decimal(0);
        this.portfolioRows.forEach(row => {
            if (row.holdingsPrice.gt(0)) {
                value = value.plus(row.holdingsPrice);
            }
        });
        this.portfolioValue = value;
    }

    private setProfitLoss() {
        let value: Decimal = new Decimal(0);
        this.portfolioRows.forEach(row => {
            value = value.plus(row.profitLoss);
        });
        this.profitLoss = value;
    }

    private setProfitLossPercentage() {
        this.profitLossPercentage = this.profitLoss.div(this.portfolioValue).mul(100);
    }

    getAllocation(holdingsPrice: Decimal) {
        let percentage: Decimal = holdingsPrice.div(this.portfolioValue).mul(100);
        return percentage.gte(0) ? percentage : 0;
    }

    setProfitLossPerYear() {
        const transactions = this.transactionService.getTransactions();
        const initialInventory: Inventory = this.transactionService.initializeInventory(transactions);
        const annotatedTransactions = this.transactionService.getAnnotatedTransactions(transactions, initialInventory);

        const years: number[] = Array.from(new Set(annotatedTransactions.map(transaction => new Date(transaction.date).getFullYear())));
        years.forEach(year => {
            this.profitLossPerYear[year] = this.transactionService.getProfitLossForYear(annotatedTransactions, year);
        });
    }
}
