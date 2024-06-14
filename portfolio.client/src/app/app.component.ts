import { Component, OnInit } from '@angular/core';
import { ApiService } from './services/api.service';
import { Coin, CoinGeckoService } from './services/coin-gecko.service';
import { MatDialog } from '@angular/material/dialog';
import { NewTransaction, NewTransactionComponent } from './new-transaction/new-transaction.component';
import { TradeService } from './services/trade.service';
import { Inventory } from './models/inventory';
import Decimal from 'decimal.js';
import { ErApiService } from './services/er-api.service';

export type PortfolioRow = {
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
    selector: 'app-root',
    templateUrl: './app.component.html',
    styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit {

    displayedColumns: string[] = ['assetId', 'price', 'holdings', 'avgBuyPrice', 'profitLoss', 'percentage'];
    portfolioRows: PortfolioRow[] = [];
    portfolioValue: Decimal = new Decimal(0);
    profitLoss: Decimal = new Decimal(0);
    profitLossPercentage: Decimal = new Decimal(0);
    profitLoss2023: Decimal = new Decimal(0);
    currencySymbol: string = "$";
    eurToUsd: Decimal = new Decimal(0);

    // Inteface control
    isLoading: boolean = true;

    constructor(
        private api: ApiService,
        private coinGecko: CoinGeckoService,
        private dialog: MatDialog,
        private tradeService: TradeService,
        private erApi: ErApiService,
    ) { }

    async ngOnInit() {
        await this.tradeService.loadData();
        await this.coinGecko.loadData(this.tradeService.assets);

        await this.loadTable();
        this.setPortfolioValue();
        this.setProfitLoss();
        this.setProfitLossPercentage();
        this.setProfitLoss2023();
        this.eurToUsd = await this.erApi.getEur2Usd();
    }

    async loadTable() {
        const assets: string[] = this.tradeService.assets;

        for (let index = 0; index < assets.length; index++) {
            const asset = assets[index];
            const holdings = this.tradeService.getHoldingsByAsset(asset);

            let coin: Coin | undefined = this.coinGecko.coins.find(c => c.id == asset);

            let price: any = this.coinGecko.prices[asset]?.usd;
            price = price ? price : 0;

            let holdingsPrice: Decimal = holdings.mul(price);
            let profitLoss: Decimal = this.tradeService.getProfitLossInEurosByAsset(asset);
            let newRow: PortfolioRow = {
                name: coin?.name ?? asset,
                symbol: coin?.symbol.toUpperCase() ?? '',
                price: price,
                holdingsPrice: holdingsPrice,
                holdingsAmount: holdings,
                avgBuyPrice: this.tradeService.getAvgBuyPriceByAsset(asset),
                profitLoss: profitLoss,
                profitLossPercentage: this.tradeService.getProfitLossPercentageByAsset(asset),
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

            this.api.postTransaction(newTransaction).then(async result => {
                await this.loadTable();
            }).catch(error => {
                alert(error);
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

    setProfitLoss2023() {
        const tradesBefore2023 = this.tradeService.getTradesBeforeYear(2023);
        const initialInventory: Inventory = this.tradeService.initializeInventory(tradesBefore2023);

        const trades2023 = this.tradeService.getTradesByYear(2023);
        this.profitLoss2023 = this.tradeService.calculateProfitsLosses(trades2023, initialInventory);
    }
}
