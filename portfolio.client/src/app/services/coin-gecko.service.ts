import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { lastValueFrom } from 'rxjs';
import { ErApiService } from './er-api.service';


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
};

@Injectable({
    providedIn: 'root'
})
export class CoinGeckoService {

    private baseUrl: string = "https://api.coingecko.com/api/v3/";
    private strEur: string = "eur";
    private strUsd: string = "usd";

    constructor(
        private http: HttpClient,
        private erApi: ErApiService,
    ) { }

    // Simple
    prefixSimple: string = "simple/";
    private getPrice(coinIds: string[], vsCurrencies: string[]) {
        return lastValueFrom(
            this.http.get<any>(this.baseUrl + this.prefixSimple + "price?ids=" + coinIds.join(",") + "&vs_currencies=" + vsCurrencies.join(","))
        );
    }

    async getPrices(assetIds: string[]) {
        let eur2usd: number = await this.erApi.getEur2Usd();
        let prices = await this.getPrice(assetIds, [this.strUsd, this.strEur]);
        prices[this.strEur] = {
            usd: eur2usd,
            eur: 1
        };
        prices[this.strUsd] = {
            usd: 1,
            eur: 1 / eur2usd
        };
        return prices;
    }

    getSupportedVsCurrencies() {
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

    async getCoins() {
        let coins: Coin[] = await this.getCoinsList();
        coins = coins.filter(coin => coin.id !== this.strEur && coin.id !== this.strUsd);
        let eur: Coin = {
            id: this.strEur,
            symbol: this.strEur,
            name: 'Euro'
        };
        let usd: Coin = {
            id: this.strUsd,
            symbol: this.strUsd,
            name: 'US Dollar'
        };
        coins.push(...[eur, usd]);
        return coins;
    }
}
