import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { lastValueFrom } from 'rxjs';
import { AssetWithHoldings } from '../models/asset-with-holdings';

@Injectable({
    providedIn: 'root'
})
export class ApiService {

    private baseUrl: string = "https://localhost:7129/api/";

    constructor(
        private http: HttpClient,
    ) { }

    getAssetsWithHoldings() {
        return lastValueFrom(
            this.http.get<AssetWithHoldings[]>(this.baseUrl + "trade/" + "getAssetsWithHoldings")
        );
    }
}
