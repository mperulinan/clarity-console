import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { lastValueFrom } from 'rxjs';
import { NewTransaction } from '../new-transaction/new-transaction.component';
import { Trade } from '../models/trade';

@Injectable({
    providedIn: 'root'
})
export class ApiService {

    private baseUrl: string = "https://localhost:7129/api/";

    constructor(
        private http: HttpClient,
    ) { }

    prefixTrade: string = "trade/"
    getTrades() {
        return lastValueFrom(
            this.http.get<Trade[]>(this.baseUrl + this.prefixTrade)
        );
    }

    postTransaction(newTransaction: NewTransaction) {
        return lastValueFrom(
            this.http.post<any>(this.baseUrl + this.prefixTrade, newTransaction)
        );
    }

    //Test
    postPricesInEur() {
        return lastValueFrom(
            this.http.post<any>(this.baseUrl + this.prefixTrade + "postPricesInEur", null)
        );
    }
}
