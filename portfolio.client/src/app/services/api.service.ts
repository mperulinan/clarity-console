import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { lastValueFrom } from 'rxjs';
import { Trade } from '../models/trade';
import { TransactionType } from '../models/transaction-type';
import { NewTransaction } from '../models/new-transaction';
import { AppSettings } from '../models/app-settings';

@Injectable({
    providedIn: 'root'
})
export class ApiService {

    private baseUrl: string = "https://localhost:7129/api/";
    public appSettings: AppSettings = {
        transactionType: {
            swap: '',
            transferIn: ''
        }
    };

    constructor(
        private http: HttpClient,
    ) {
        this.loadAppSettings();
    }

    async loadAppSettings() {
        this.getAppSetting("TransactionType:Swap").then((value) => this.appSettings.transactionType.swap = value).catch((error) => { alert("Error. See console."); console.log(error); });
        this.getAppSetting("TransactionType:TransferIn").then((value) => this.appSettings.transactionType.transferIn = value).catch((error) => { alert("Error. See console."); console.log(error); });
    }


    prefixAppSettings: string = "appSettings/";
    getAppSetting(settingName: string) {
        return lastValueFrom(
            this.http.get(this.baseUrl + this.prefixAppSettings + settingName, { responseType: 'text' })
        );
    }


    prefixTrade: string = "trade/";
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


    prefixTransactionType: string = "transactionType/";
    getTransactionTypes() {
        return lastValueFrom(
            this.http.get<TransactionType[]>(this.baseUrl + this.prefixTransactionType)
        );
    }
}
