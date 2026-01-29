import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { lastValueFrom } from 'rxjs';
import { Transaction } from '../models/transaction';
import { ProcessedTransaction } from '../models/processed-transaction';
import { TransactionType } from '../models/transaction-type';
import { NewTransaction } from '../models/new-transaction';
import { AppSettings } from '../models/app-settings';
import { environment } from '../../../environments/environment';

@Injectable({
    providedIn: 'root'
})
export class ApiService {

    private baseUrl: string = environment.appUrl + "api/";
    public appSettings: AppSettings = {
        transactionType: {
            swap: '',
            transferIn: '',
            reward: ''
        }
    };

    constructor(
        private http: HttpClient,
    ) {
        this.loadAppSettings();
    }

    async loadAppSettings() {
        try {
            this.appSettings.transactionType.swap = await this.getAppSetting("TransactionType:Swap");
            this.appSettings.transactionType.transferIn = await this.getAppSetting("TransactionType:TransferIn");
            this.appSettings.transactionType.reward = await this.getAppSetting("TransactionType:Reward");
        } catch (e) {
            console.error("Failed to load settings", e);
        }
    }

    prefixAppSettings: string = "appSettings/";
    getAppSetting(settingName: string) {
        return lastValueFrom(
            this.http.get(this.baseUrl + this.prefixAppSettings + settingName, { responseType: 'text' })
        );
    }

    prefixTransaction: string = "transaction/";
    getTransactions() {
        return lastValueFrom(
            this.http.get<ProcessedTransaction[]>(this.baseUrl + this.prefixTransaction)
        );
    }

    postTransaction(newTransaction: NewTransaction) {
        return lastValueFrom(
            this.http.post<any>(this.baseUrl + this.prefixTransaction, newTransaction)
        );
    }

    postPricesInEur() {
        return lastValueFrom(
            this.http.post<any>(this.baseUrl + this.prefixTransaction + "postPricesInEur", null)
        );
    }


    prefixTransactionType: string = "transactionType/";
    getTransactionTypes() {
        return lastValueFrom(
            this.http.get<TransactionType[]>(this.baseUrl + this.prefixTransactionType)
        );
    }
}
