import { Component, OnInit } from '@angular/core';
import { ApiService } from './services/api.service';
import { AssetWithHoldings } from './models/asset-with-holdings';
import { CoinGeckoService } from './services/coin-gecko.service';

export type PortfolioRow = {
    name: string,
    symbol: string,
    price: string,
    holdingsPrice: number,
    holdingsAmount: number,
};

@Component({
    selector: 'app-root',
    templateUrl: './app.component.html',
    styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit {
    title = 'portfolio';

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

        let prices = await this.coinGecko.getPrice(assetIds, ["usd", "eur"]);
        console.log("coinGeckoinfo", prices);


        this.assetsWithHoldings.forEach(asset => {
            if (asset.assetId != "EUR" && asset.assetId != "USD") {
                this.portfolioRows.push({
                    name: asset.assetId,
                    symbol: 'n/a',
                    price: prices[asset.assetId]["usd"],
                    holdingsPrice: asset.holdings * prices[asset.assetId].usd,
                    holdingsAmount: asset.holdings
                });
            }
        });

        this.isLoading = false;
    }
}
