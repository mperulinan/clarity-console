import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { lastValueFrom } from 'rxjs';

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
            this.http.get<any>(this.baseUrl + "trade/" + "getAssetsWithHoldings")
        );
    }
}
