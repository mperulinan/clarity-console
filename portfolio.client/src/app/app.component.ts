import { Component, OnInit } from '@angular/core';
import { ApiService } from './services/api.service';
import { AssetWithHoldings } from './models/asset-with-holdings';
import { Coin, CoinGeckoService } from './services/coin-gecko.service';

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

    // Inteface control
    isLoading: boolean = true;

    constructor(
        private api: ApiService,
        private coinGecko: CoinGeckoService,
    ) { }

    async ngOnInit() {
        this.assetsWithHoldings = await this.api.getAssetsWithHoldings();
        console.log(this.assetsWithHoldings);

        let assetIds: string[] = this.assetsWithHoldings.map(a => a.assetId);
        let prices = await this.coinGecko.getPrices(assetIds);
        console.log("prices", prices);

        let coins: Coin[] = await this.coinGecko.getCoins();
        console.log("coins", coins);


        for (let index = 0; index < this.assetsWithHoldings.length; index++) {
            const asset = this.assetsWithHoldings[index];

            let coin: Coin | undefined = coins.find(c => c.id == asset.assetId);
            let newRow: PortfolioRow = {
                name: coin?.name ?? asset.assetId,
                symbol: coin?.symbol.toUpperCase() ?? '',
                price: prices[asset.assetId].usd,
                holdingsPrice: asset.holdings * prices[asset.assetId].usd,
                holdingsAmount: asset.holdings
            };
            this.portfolioRows.push(newRow);
        }

        this.isLoading = false;
    }
}
