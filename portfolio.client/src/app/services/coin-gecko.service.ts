import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { lastValueFrom } from 'rxjs';


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

    constructor(
        private http: HttpClient,
    ) { }

    // Simple
    prefixSimple: string = "simple/";
    getPrice(coinIds: string[], vsCurrencies: string[]) {
        return lastValueFrom(
            this.http.get<any>(this.baseUrl + this.prefixSimple + "price?ids=" + coinIds.join(",") + "&vs_currencies=" + vsCurrencies.join(","))
        );
    }

    getSupportedVsCurrencies() {
        return lastValueFrom(
            this.http.get<string[]>(this.baseUrl + this.prefixSimple + "supported_vs_currencies")
        );
    }


    // Coins
    prefixCoins: string = "coins/";
    getCoinsList() {
        return lastValueFrom(
            this.http.get<Coin[]>(this.baseUrl + this.prefixCoins + "list")
        );
    }
}
