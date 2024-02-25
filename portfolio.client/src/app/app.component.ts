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

    displayedColumns: string[] = ['assetId', 'price', 'holdings'];
    portfolioRows: PortfolioRow[] = [];
    assetsWithHoldings: AssetWithHoldings[] = [];
    coins: Coin[] = [];

    // Inteface control
    isLoading: boolean = true;

    constructor(
        private api: ApiService,
        private coinGecko: CoinGeckoService,
        private dialog: MatDialog,
    ) { }

    async ngOnInit() {
        await this.loadTable();
    }

    async loadTable() {
        this.assetsWithHoldings = await this.api.getAssetsWithHoldings();
        console.log(this.assetsWithHoldings);

        let assetIds: string[] = this.assetsWithHoldings.map(a => a.assetId);
        let prices = await this.coinGecko.getPrices(assetIds);
        console.log("prices", prices);

        this.coins = await this.coinGecko.getCoins();
        console.log("coins", this.coins);


        for (let index = 0; index < this.assetsWithHoldings.length; index++) {
            const asset = this.assetsWithHoldings[index];

            let coin: Coin | undefined = this.coins.find(c => c.id == asset.assetId);
            let newRow: PortfolioRow = {
                name: coin?.name ?? asset.assetId,
                symbol: coin?.symbol.toUpperCase() ?? '',
                price: prices[asset.assetId]?.usd,
                holdingsPrice: asset.holdings * prices[asset.assetId]?.usd,
                holdingsAmount: asset.holdings
            };
            this.portfolioRows.push(newRow);
        }

        this.isLoading = false;
    }

    openNewTransactionDialog() {
        let dialogRef = this.dialog.open(NewTransactionComponent, {
            width: '500px',
            data: { coins: this.coins },
        });

        dialogRef.afterClosed().subscribe(async (newTransaction: NewTransaction) => {
            this.api.postTransaction(newTransaction).then(async result => {
                await this.loadTable();
            }).catch(error => {
                alert(error);
            });
        });
    }
}
