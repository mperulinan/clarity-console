import { Component, Inject, ChangeDetectionStrategy } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { CommonModule, CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { ProcessedTransaction } from '../../../models/transaction';

export interface WashSaleDialogData {
    lossTx: ProcessedTransaction;
    repurchaseTx: ProcessedTransaction;
    currency: string;
}

@Component({
    selector: 'app-wash-sale-details-dialog',
    standalone: true,
    imports: [CommonModule, MatDialogModule, MatButtonModule, MatIconModule, CurrencyPipe, DatePipe, DecimalPipe],
    templateUrl: './wash-sale-details-dialog.html',
    styleUrl: './wash-sale-details-dialog.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class WashSaleDetailsDialogComponent {
    constructor(
        public dialogRef: MatDialogRef<WashSaleDetailsDialogComponent>,
        @Inject(MAT_DIALOG_DATA) public data: WashSaleDialogData
    ) { }

    getSpotPrice(tx: ProcessedTransaction): number | undefined {
        return this.data.currency === 'EUR' ? tx.transaction.spotPriceEUR : tx.transaction.spotPriceUSD;
    }

    close() {
        this.dialogRef.close();
    }
}
