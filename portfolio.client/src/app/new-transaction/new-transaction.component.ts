import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { Coin } from '../services/coin-gecko.service';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { Observable, map, startWith } from 'rxjs';

export interface DialogData {
    coins: Coin[];
}

export interface NewTransaction {
    fromAssetId: string,
    toAssetId: string,
    amountSpent: number,
    amountReceived: number,
    fee: number,
    date: Date,
    notes: string | null,
};

@Component({
    selector: 'app-new-transaction',
    templateUrl: './new-transaction.component.html',
    styleUrl: './new-transaction.component.scss'
})
export class NewTransactionComponent {

    fromAsset: FormControl<string | null> = new FormControl(null, Validators.required);
    toAsset: FormControl<string | null> = new FormControl(null, Validators.required);
    amountSpent: FormControl<number | null> = new FormControl(null, Validators.required);
    amountReceived: FormControl<number | null> = new FormControl(null, Validators.required);
    fee: FormControl<number | null> = new FormControl(0, Validators.required);
    date: FormControl<Date | null> = new FormControl(new Date(), Validators.required);
    hour: FormControl<number | null> = new FormControl(null, Validators.required);
    minute: FormControl<number | null> = new FormControl(null, Validators.required);
    notes: FormControl<string | null> = new FormControl('');
    transactionForm = new FormGroup({
        fromAsset: this.fromAsset,
        toAsset: this.toAsset,
        amountSpent: this.amountSpent,
        amountReceived: this.amountReceived,
        fee: this.fee,
        date: this.date,
        hour: this.hour,
        minute: this.minute,
        notes: this.notes,
    });

    // Data
    filteredCoinsFrom!: Observable<Coin[]>;
    filteredCoinsTo!: Observable<Coin[]>;

    constructor(
        public dialogRef: MatDialogRef<NewTransactionComponent>,
        @Inject(MAT_DIALOG_DATA) private data: DialogData
    ) { }

    ngOnInit() {
        this.filteredCoinsFrom = this.fromAsset.valueChanges.pipe(
            map(value => this._filter(value || '').slice(0, 5)),
        );
        this.filteredCoinsTo = this.toAsset.valueChanges.pipe(
            map(value => this._filter(value || '').slice(0, 5)),
        );
    }

    add(): void {
        if (this.transactionForm.invalid || !this.fromAsset.value || !this.toAsset.value) {
            return;
        }

        let newTransaction: NewTransaction = {
            fromAssetId: this.fromAsset.value,
            toAssetId: this.toAsset.value,
            amountSpent: this.amountSpent.value ?? 0,
            amountReceived: this.amountReceived.value ?? 0,
            fee: this.fee.value ?? 0,
            date: this.combineDateTime(),
            notes: this.notes.value
        };

        this.dialogRef.close(newTransaction);
    }

    combineDateTime(): Date {
        const combinedDate: Date = new Date(this.date.value || new Date());
        combinedDate.setHours(this.hour.value || 0);
        combinedDate.setMinutes(this.minute.value || 0);
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
