import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { CookieService } from 'ngx-cookie-service';
import { lastValueFrom } from 'rxjs';

export type ERRepsonse = {
    rates: {
        USD: number,
        EUR: number
    }
};

@Injectable({
    providedIn: 'root'
})
export class ErApiService {

    private baseUrl: string = "https://open.er-api.com/v6/latest/EUR";
    private cookieName: string = "portfolio_eur2usd";

    constructor(
        private http: HttpClient,
        private cookieService: CookieService,
    ) { }

    private getData() {
        return lastValueFrom(
            this.http.get<ERRepsonse>(this.baseUrl)
        );
    }

    async getEur2Usd() {
        let value: string = this.cookieService.get(this.cookieName);

        if (!value) {
            let erData: ERRepsonse = await this.getData();
            value = erData.rates.USD.toString();
            this.cookieService.set(this.cookieName, value, 0.1);
        }

        return parseFloat(value);
    }
}
