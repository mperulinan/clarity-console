import { Component, OnInit } from '@angular/core';
import { ApiService } from './services/api.service';
import { AssetWithHoldings } from './models/asset-with-holdings';
import { Coin, CoinGeckoService } from './services/coin-gecko.service';
import { MatDialog } from '@angular/material/dialog';
import { NewTransaction, NewTransactionComponent } from './new-transaction/new-transaction.component';

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
    assetsWithHoldings: AssetWithHoldings[] = [];
    coins: Coin[] = [];
    portfolioValue: number = 0;

    // Inteface control
    isLoading: boolean = true;

    constructor(
        private api: ApiService,
        private coinGecko: CoinGeckoService,
        private dialog: MatDialog,
    ) { }

    async ngOnInit() {
        await this.loadTable();
        this.setPortfolioValue();
    }

    async loadTable() {
        this.assetsWithHoldings = await this.api.getAssetsWithHoldings();
        console.log("assetsWithHoldings", this.assetsWithHoldings);

        let assetIds: string[] = this.assetsWithHoldings.map(a => a.assetId);
        let prices = await this.coinGecko.getPrices(assetIds);
        console.log("prices", prices);

        this.coins = await this.coinGecko.getCoins();
        console.log("coins", this.coins);


        for (let index = 0; index < this.assetsWithHoldings.length; index++) {
            const asset = this.assetsWithHoldings[index];

            let coin: Coin | undefined = this.coins.find(c => c.id == asset.assetId);
            let holdingsPrice: number = asset.holdings * prices[asset.assetId]?.usd
            let newRow: PortfolioRow = {
                name: coin?.name ?? asset.assetId,
                symbol: coin?.symbol.toUpperCase() ?? '',
                price: prices[asset.assetId]?.usd,
                holdingsPrice: holdingsPrice,
                holdingsAmount: asset.holdings,
            };
            this.portfolioRows.push(newRow);
        }

        this.portfolioRows.sort((a, b) => b.holdingsPrice - a.holdingsPrice);

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

    getPercentage(holdingsPrice: number) {
        let percentage: number = holdingsPrice / this.portfolioValue * 100;
        return percentage >= 0 ? percentage : 0;
    }
}
