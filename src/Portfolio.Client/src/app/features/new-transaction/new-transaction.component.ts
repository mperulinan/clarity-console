import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';

// Material
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonToggleModule } from '@angular/material/button-toggle';

import { PortfolioService } from '../../services/portfolio.service';
import { NewTransactionRequest } from '../../models/new-transaction-request';

@Component({
    selector: 'app-new-transaction',
    standalone: true,
    imports: [
        CommonModule,
        ReactiveFormsModule,
        MatCardModule,
        MatFormFieldModule,
        MatInputModule,
        MatSelectModule,
        MatDatepickerModule,
        MatButtonModule,
        MatIconModule,
        MatButtonToggleModule
    ],
    templateUrl: './new-transaction.component.html',
    styleUrl: './new-transaction.component.scss'
})
export class NewTransactionComponent {
    form: FormGroup;
    isSubmitting = false;

    transactionTypes = [
        { value: 'SWAP', label: 'Buy / Sell / Swap' },
        // { value: 'TRANSFER', label: 'Transfer' }, // TODO: Implement later
        // { value: 'REWARD', label: 'Reward' }
    ];

    constructor(
        private fb: FormBuilder,
        private portfolioService: PortfolioService,
        private router: Router
    ) {
        this.form = this.fb.group({
            date: [new Date(), Validators.required],
            type: ['SWAP', Validators.required],
            fromAssetId: ['', Validators.required],
            toAssetId: ['', Validators.required],
            amountSpent: [null, [Validators.required, Validators.min(0)]],
            amountReceived: [null, [Validators.required, Validators.min(0)]],
            fromAssetPriceInUsd: [null],
            fee: [0],
            feeAsset: [''],
            feeAssetPriceInUsd: [null],
            notes: ['']
        });
    }

    onSubmit() {
        if (this.form.invalid || this.isSubmitting) return;

        this.isSubmitting = true;
        const formValue = this.form.value;

        const request: NewTransactionRequest = {
            date: formValue.date.toISOString(),
            transactionTypeCode: formValue.type,
            fromAssetId: formValue.fromAssetId.toUpperCase(),
            toAssetId: formValue.toAssetId.toUpperCase(),
            amountSpent: Number(formValue.amountSpent),
            amountReceived: Number(formValue.amountReceived),
            fromAssetPriceInUsd: formValue.fromAssetPriceInUsd ? Number(formValue.fromAssetPriceInUsd) : undefined,
            fee: Number(formValue.fee || 0),
            feeAsset: formValue.feeAsset ? formValue.feeAsset.toUpperCase() : undefined,
            feeAssetPriceInUsd: formValue.feeAssetPriceInUsd ? Number(formValue.feeAssetPriceInUsd) : undefined,
            notes: formValue.notes
        };

        this.portfolioService.addTransaction(request).subscribe({
            next: () => {
                this.router.navigate(['/']); // Back to Dashboard
            },
            error: (err) => {
                console.error('Failed to save transaction', err);
                this.isSubmitting = false;
            }
        });
    }

    onCancel() {
        this.router.navigate(['/']);
    }
}
