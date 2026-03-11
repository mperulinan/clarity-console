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
        { value: 'SWAP', label: 'Swap' },
        { value: 'DEPOSIT', label: 'Deposit' },
        { value: 'WITHDRAWAL', label: 'Withdrawal' },
        { value: 'REWARD', label: 'Reward' }
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
            spotPriceInUsd: [null, Validators.required],
            fee: [0],
            feeAssetId: [''],
            feeSpotPriceInUsd: [null],
            notes: ['']
        });

        this.onTypeChange('SWAP'); // Initial setup
    }

    onTypeChange(type: string) {
        const fromControl = this.form.get('fromAssetId');
        const toControl = this.form.get('toAssetId');
        const spentControl = this.form.get('amountSpent');
        const receivedControl = this.form.get('amountReceived');

        // Reset validators
        fromControl?.clearValidators();
        toControl?.clearValidators();
        spentControl?.clearValidators();
        receivedControl?.clearValidators();

        if (type === 'SWAP') {
            fromControl?.setValidators(Validators.required);
            toControl?.setValidators(Validators.required);
            spentControl?.setValidators([Validators.required, Validators.min(0)]);
            receivedControl?.setValidators([Validators.required, Validators.min(0)]);
        } else if (type === 'DEPOSIT' || type === 'REWARD') {
            toControl?.setValidators(Validators.required);
            receivedControl?.setValidators([Validators.required, Validators.min(0)]);
            spentControl?.setValue(0);
        } else if (type === 'WITHDRAWAL') {
            fromControl?.setValidators(Validators.required);
            spentControl?.setValidators([Validators.required, Validators.min(0)]);
            receivedControl?.setValue(0);
        }

        fromControl?.updateValueAndValidity();
        toControl?.updateValueAndValidity();
        spentControl?.updateValueAndValidity();
        receivedControl?.updateValueAndValidity();
    }

    onSubmit() {
        if (this.form.invalid || this.isSubmitting) return;

        this.isSubmitting = true;
        const formValue = this.form.value;

        const request: NewTransactionRequest = {
            date: formValue.date.toISOString(),
            transactionTypeCode: formValue.type,
            fromAssetId: formValue.fromAssetId || undefined,
            toAssetId: formValue.toAssetId || undefined,
            amountSpent: Number(formValue.amountSpent || 0),
            amountReceived: Number(formValue.amountReceived || 0),
            spotPriceInUsd: Number(formValue.spotPriceInUsd),
            fee: Number(formValue.fee || 0),
            feeAssetId: formValue.feeAssetId || undefined,
            feeSpotPriceInUsd: formValue.feeSpotPriceInUsd ? Number(formValue.feeSpotPriceInUsd) : undefined,
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
