import { Component, OnInit } from '@angular/core';
import { ApiService } from './services/api.service';
import { AssetWithHoldings } from './models/asset-with-holdings';
import { Coin, CoinGeckoService, Price } from './services/coin-gecko.service';
import { ErApiService } from './services/er-api.service';

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
        private erApi: ErApiService,
    ) { }

    async ngOnInit() {
        this.assetsWithHoldings = await this.api.getAssetsWithHoldings();
        console.log(this.assetsWithHoldings);

        let assetIds: string[] = this.assetsWithHoldings.map(a => a.assetId);

        let eur2usd: number = await this.erApi.getEur2Usd();
        let prices = await this.coinGecko.getPrice(assetIds, ["usd", "eur"]);
        prices["eur"] = {
            usd: eur2usd,
            eur: 1
        };
        prices["usd"] = {
            usd: 1,
            eur: 1 / eur2usd
        };
        console.log("prices", prices);

        let coins: Coin[] = await this.coinGecko.getCoinsList();
        coins = coins.filter(coin => coin.id !== 'eur' && coin.id !== 'usd');
        let eur: Coin = {
            id: 'eur',
            symbol: 'eur',
            name: 'Euro'
        };
        let usd: Coin = {
            id: 'usd',
            symbol: 'usd',
            name: 'US Dollar'
        };
        coins.push(...[eur, usd]);
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
