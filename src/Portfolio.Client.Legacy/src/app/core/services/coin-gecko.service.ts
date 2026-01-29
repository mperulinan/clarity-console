import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { lastValueFrom } from 'rxjs';
import { ErApiService } from './er-api.service';
import Decimal from 'decimal.js';
import { Coin } from '../models/coin';


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

    public async getAllCoins(): Promise<Coin[]> {
        if (this.coins.length > 0) return this.coins;
        return await this.getCoinsList();
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

    private getCoinsWithMarketData(resultsPerPage: number, pageNum: number) {
        return lastValueFrom(
            this.http.get<Coin[]>(this.baseUrl + this.prefixCoins + markets?vs_currency=&per_page=&page=)
        );
    }

    private getMyCoinsWithMarketData(coinIds: string[]) {
        return lastValueFrom(
            this.http.get<Coin[]>(this.baseUrl + this.prefixCoins + markets?vs_currency=&ids=)
        );
    }

    private async getCoins(coinsIds: string[]) {
        let coins: Coin[] = await this.getMyCoinsWithMarketData(coinsIds);

        for (let i = 1; i <= 1; i++) {
            coins = coins.concat(await this.getCoinsWithMarketData(250, i));
        }

        coins = this.removeDuplicatedCoins(coins);
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

    private removeDuplicatedCoins(coins: Coin[]): Coin[] {
        const uniqueCoinsMap = new Map<string, Coin>();
        coins.forEach((coin) => {
            uniqueCoinsMap.set(coin.id, coin);
        });
        return Array.from(uniqueCoinsMap.values());
    }
}
