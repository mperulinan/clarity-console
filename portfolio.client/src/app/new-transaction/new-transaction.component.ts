import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { Observable, map, startWith } from 'rxjs';
import { TransactionType } from '../models/transaction-type';
import { ApiService } from '../services/api.service';
import { NewTransaction } from '../models/new-transaction';
import { Coin } from '../models/coin';

export interface DialogData {
    coins: Coin[];
}

@Component({
    selector: 'app-new-transaction',
    templateUrl: './new-transaction.component.html',
    styleUrl: './new-transaction.component.scss'
})
export class NewTransactionComponent {

    transactionType: FormControl<string | null> = new FormControl(null, Validators.required);
    fromAsset: FormControl<string | null> = new FormControl(null, Validators.required);
    toAsset: FormControl<string | null> = new FormControl(null, Validators.required);
    amountSpent: FormControl<number | null> = new FormControl(0, Validators.required);
    amountReceived: FormControl<number | null> = new FormControl(0, Validators.required);
    fromAssetPriceInEur: FormControl<number | null> = new FormControl(null, Validators.required);
    fee: FormControl<number | null> = new FormControl(0, Validators.required);
    feeAsset: FormControl<string | null> = new FormControl(null);
    feeAssetPriceInEur: FormControl<number | null> = new FormControl(0);
    date: FormControl<Date | null> = new FormControl(new Date(), Validators.required);
    hour: FormControl<number | null> = new FormControl(0, Validators.required);
    minute: FormControl<number | null> = new FormControl(0, Validators.required);
    second: FormControl<number | null> = new FormControl(0, Validators.required);
    notes: FormControl<string | null> = new FormControl('');
    transactionForm = new FormGroup({
        transactionType: this.transactionType,
        fromAsset: this.fromAsset,
        toAsset: this.toAsset,
        amountSpent: this.amountSpent,
        amountReceived: this.amountReceived,
        fromAssetPriceInEur: this.fromAssetPriceInEur,
        fee: this.fee,
        feeAsset: this.feeAsset,
        feeAssetPriceInEur: this.feeAssetPriceInEur,
        date: this.date,
        hour: this.hour,
        minute: this.minute,
        second: this.second,
        notes: this.notes,
    });

    // Data
    transactionTypes: TransactionType[] = [];
    filteredCoinsFrom!: Observable<Coin[]>;
    filteredCoinsTo!: Observable<Coin[]>;
    filteredCoinsFee!: Observable<Coin[]>;

    constructor(
        public dialogRef: MatDialogRef<NewTransactionComponent>,
        @Inject(MAT_DIALOG_DATA) private data: DialogData,
        private apiService: ApiService,
    ) { }

    async ngOnInit() {
        this.transactionTypes = await this.apiService.getTransactionTypes().catch((error) => {
            alert(error);
            return [];
        });

        this.filteredCoinsFrom = this.fromAsset.valueChanges.pipe(
            map(value => this._filter(value || '').slice(0, 5)),
        );
        this.filteredCoinsTo = this.toAsset.valueChanges.pipe(
            map(value => this._filter(value || '').slice(0, 5)),
        );
        this.filteredCoinsFee = this.feeAsset.valueChanges.pipe(
            map(value => this._filter(value || '').slice(0, 5)),
        );
    }

    add(): void {
        if (!this.transactionForm.valid || !this.transactionType.value || !this.fromAsset.value || !this.toAsset.value) {
            return;
        }

        let newTransaction: NewTransaction = {
            date: this.combineDateTime(),
            transactionType: this.transactionType.value,
            fromAssetId: this.fromAsset.value,
            toAssetId: this.toAsset.value,
            amountSpent: this.amountSpent.value?.toString() ?? "0",
            amountReceived: this.amountReceived.value?.toString() ?? "0",
            fromAssetPriceInEur: this.fromAssetPriceInEur.value?.toString() ?? "0",
            fee: this.fee.value?.toString() ?? "0",
            feeAsset: this.feeAsset.value,
            feeAssetPriceInEur: this.feeAssetPriceInEur.value?.toString() ?? null,
            notes: this.notes.value,
        };

        this.dialogRef.close(newTransaction);
    }

    combineDateTime(): Date {
        const combinedDate: Date = new Date(this.date.value || new Date());
        combinedDate.setHours(this.hour.value || 0);
        combinedDate.setMinutes(this.minute.value || 0);
        combinedDate.setSeconds(this.second.value || 0);
        return combinedDate;
    }

    private _filter(value: string): Coin[] {
        const filterValue = value.toLowerCase();
        let coins = this.data.coins.filter(c => c.symbol.toLowerCase() == filterValue);
        if (coins.length == 0) {
            coins = this.data.coins.filter(c => c.name.toLowerCase() == filterValue);
        }
        if (coins.length == 0) {
            coins = this.data.coins.filter(c => c.symbol.toLowerCase().startsWith(filterValue));
        }
        if (coins.length == 0) {
            coins = this.data.coins.filter(c => c.name.toLowerCase().startsWith(filterValue));
        }
        if (coins.length == 0) {
            coins = this.data.coins.filter(c => c.symbol.toLowerCase().includes(filterValue));
        }
        if (coins.length == 0) {
            coins = this.data.coins.filter(c => c.name.toLowerCase().includes(filterValue));
        }

        return coins;
    }
}
