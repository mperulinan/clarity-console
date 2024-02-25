import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { lastValueFrom } from 'rxjs';
import { AssetWithHoldings } from '../models/asset-with-holdings';
import { NewTransaction } from '../new-transaction/new-transaction.component';

@Injectable({
    providedIn: 'root'
})
export class ApiService {

    private baseUrl: string = "https://localhost:7129/api/";

    constructor(
        private http: HttpClient,
    ) { }

    prefixTrade: string = "trade/"
    getAssetsWithHoldings() {
        return lastValueFrom(
            this.http.get<AssetWithHoldings[]>(this.baseUrl + this.prefixTrade + "getAssetsWithHoldings")
        );
    }

    postTransaction(newTransaction: NewTransaction) {
        return lastValueFrom(
            this.http.post<any>(this.baseUrl + this.prefixTrade, newTransaction)
        );
    }
}
