import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { AnnotatedTransaction } from '../models/annotated-transaction';
import { ProcessedTransaction } from '../models/processed-transaction';

@Injectable({
    providedIn: 'root'
})
export class TransactionService {

    constructor(
        private api: ApiService,
    ) { }

    async getTransactions(): Promise<AnnotatedTransaction[]> {
        const dtos = await this.api.getTransactions();
        return dtos.map(dto => ({
            ...dto.transaction,
            profitLoss: dto.profitLoss,
            totalLossAmount: dto.totalLossAmount,
            isLossDisallowed: dto.isLossDisallowed,
            disallowedByTransactionId: dto.disallowedByTransactionId,
            disallowsPreviousLosses: dto.disallowsPreviousLosses
        }));
    }
}
