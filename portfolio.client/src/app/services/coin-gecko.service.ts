import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { lastValueFrom } from 'rxjs';
import { ErApiService } from './er-api.service';
import Decimal from 'decimal.js';


export type Price = {
    [id: string]: {
        usd: number,
        eur: number,
    }
};
export type Coin = {
    id: string,
    symbol: string,
    name: string,
    image: string,
};

@Injectable({
    providedIn: 'root'
})
export class CoinGeckoService {

    private baseUrl: string = "https://api.coingecko.com/api/v3/";
    private strEur: string = "eur";
    private strUsd: string = "usd";

    // Data
    prices: any;
    coins: Coin[] = [];

    constructor(
        private http: HttpClient,
        private erApi: ErApiService,
    ) { }


    async loadData(assetIds: string[]) {
        this.prices = await this.getPrices(assetIds);
        this.coins = await this.getCoins(assetIds);
        console.log("coins", this.coins);
    }


    // Simple
    prefixSimple: string = "simple/";
    private getPrice(coinIds: string[], vsCurrencies: string[]) {
        return lastValueFrom(
            this.http.get<any>(this.baseUrl + this.prefixSimple + "price?ids=" + coinIds.join(",") + "&vs_currencies=" + vsCurrencies.join(","))
        );
    }

    private async getPrices(assetIds: string[]) {
        let eur2usd: Decimal = await this.erApi.getEur2Usd();
        let prices = await this.getPrice(assetIds, [this.strUsd, this.strEur]);
        prices[this.strEur] = {
            usd: eur2usd,
            eur: 1
        };
        prices[this.strUsd] = {
            usd: 1,
            eur: new Decimal(1).div(eur2usd)
        };
        return prices;
    }

    private getSupportedVsCurrencies() {
        return lastValueFrom(
            this.http.get<string[]>(this.baseUrl + this.prefixSimple + "supported_vs_currencies")
        );
    }


    // Coins
    prefixCoins: string = "coins/";
    private getCoinsList() {
        return lastValueFrom(
            this.http.get<Coin[]>(this.baseUrl + this.prefixCoins + "list")
        );
    }

    private getCoinsWithMarketData(coinIds: string[]) {
        return lastValueFrom(
            this.http.get<Coin[]>(this.baseUrl + this.prefixCoins + `markets?vs_currency=${this.strUsd}&ids=${coinIds.join(",")}`)
        );
    }

    private async getCoins(assetIds: string[]) {
        let coins: Coin[] = await this.getCoinsWithMarketData(assetIds);
        coins = coins.filter(coin => coin.id !== this.strEur && coin.id !== this.strUsd);
        let eur: Coin = {
            id: this.strEur,
            symbol: this.strEur,
            name: 'Euro',
            image: "",
        };
        let usd: Coin = {
            id: this.strUsd,
            symbol: this.strUsd,
            name: 'US Dollar',
            image: "",
        };
        coins.push(...[eur, usd]);
        return coins;
    }
}
