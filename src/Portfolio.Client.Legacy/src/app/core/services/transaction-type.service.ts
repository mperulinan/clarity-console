import { Injectable } from '@angular/core';
import { ApiService } from './api.service';

@Injectable({
    providedIn: 'root'
})
export class TransactionTypeService {

    constructor(private api: ApiService) { }

    get TYPE_SWAP(): string {
        return this.api.appSettings.transactionType.swap;
    }

    get TYPE_REWARD(): string {
        return this.api.appSettings.transactionType.reward;
    }

    get TYPE_TRANSFER(): string {
        return this.api.appSettings.transactionType.transferIn;
    }

    get options() {
        return [
            {
                label: $localize`:@@swap:Swap`,
                value: this.TYPE_SWAP,
            },
            {
                label: $localize`:@@reward:Reward`,
                value: this.TYPE_REWARD,
            },
            {
                label: $localize`:@@transfer:Transfer`,
                value: this.TYPE_TRANSFER,
            },
        ];
    }
}
