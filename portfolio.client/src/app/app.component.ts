import { Component, OnInit } from '@angular/core';
import { ApiService } from './services/api.service';
import { Coin, CoinGeckoService } from './services/coin-gecko.service';
import { MatDialog } from '@angular/material/dialog';
import { NewTransaction, NewTransactionComponent } from './new-transaction/new-transaction.component';
import { TradeService } from './services/trade.service';
import { lastValueFrom } from 'rxjs';

export type PortfolioRow = {
    name: string,
    symbol: string,
    price: number,
    holdingsPrice: number,
    holdingsAmount: number,
};

@Component({
    selector: 'app-root',
    templateUrl: './app.component.html',
    styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit {

    displayedColumns: string[] = ['assetId', 'price', 'holdings', 'percentage'];
    portfolioRows: PortfolioRow[] = [];
    coins: Coin[] = [];
    portfolioValue: number = 0;
    profit: number = 0;
    profitPercentage: number = 0;
    currencySymbol: string = "$";

    // Inteface control
    isLoading: boolean = true;

    constructor(
        private api: ApiService,
        private coinGecko: CoinGeckoService,
        private dialog: MatDialog,
        private tradeService: TradeService,
    ) { }

    async ngOnInit() {
        await this.tradeService.loadData();

        await this.loadTable();
        this.setPortfolioValue();
        this.setProfit();
        this.setProfitPercentage();
    }

    async loadTable() {
        const assets: string[] = this.tradeService.assets;

        let prices = await this.coinGecko.getPrices(assets);
        console.log("prices", prices);

        this.coins = await this.coinGecko.getCoins();
        console.log("coins", this.coins);

        for (let index = 0; index < assets.length; index++) {
            const asset = assets[index];
            const holdings = this.tradeService.getHoldingsByAsset(asset);

            let coin: Coin | undefined = this.coins.find(c => c.id == asset);

            let price: any = prices[asset]?.usd;
            price = price ? price : 0;

            let holdingsPrice: number = holdings * price;
            let newRow: PortfolioRow = {
                name: coin?.name ?? asset,
                symbol: coin?.symbol.toUpperCase() ?? '',
                price: price,
                holdingsPrice: holdingsPrice,
                holdingsAmount: holdings,
            };

            if (newRow.holdingsAmount != 0) {
                this.portfolioRows.push(newRow);
            }
        }

        this.portfolioRows.sort((a, b) => b.holdingsPrice - a.holdingsPrice);
        console.log("portfolioRows", this.portfolioRows);


        this.isLoading = false;
    }

    openNewTransactionDialog() {
        let dialogRef = this.dialog.open(NewTransactionComponent, {
            width: '500px',
            data: { coins: this.coins },
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
        let value: number = 0;
        this.portfolioRows.forEach(row => {
            if (row.holdingsPrice > 0) {
                value += row.holdingsPrice;
            }
        });
        this.portfolioValue = value;
    }

    private setProfit() {
        let value: number = 0;
        this.portfolioRows.forEach(row => {
            value += row.holdingsPrice;
        });
        this.profit = value;
    }

    private setProfitPercentage() {
        this.profitPercentage = this.profit / this.portfolioValue * 100;
    }

    getPercentage(holdingsPrice: number) {
        let percentage: number = holdingsPrice / this.portfolioValue * 100;
        return percentage >= 0 ? percentage : 0;
    }
}
