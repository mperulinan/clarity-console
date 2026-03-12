import { Component, OnInit, signal, computed, ChangeDetectionStrategy } from '@angular/core';
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
import { TransactionType } from '../../models/transaction';
import { finalize } from 'rxjs';

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
    styleUrl: './new-transaction.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class NewTransactionComponent implements OnInit {
    form: FormGroup;

    // State using Signals
    isSubmitting = signal<boolean>(false);
    isLoadingTypes = signal<boolean>(true);
    transactionTypes = signal<TransactionType[]>([]);

    // Computed state
    selectedTypeData = computed(() => {
        const typeValue = this.form.get('type')?.value;
        const types = this.transactionTypes();
        return types.find(t => t.value === typeValue);
    });

    constructor(
        private fb: FormBuilder,
        private portfolioService: PortfolioService,
        private router: Router
    ) {
        this.form = this.fb.group({
            date: [new Date(), Validators.required],
            type: ['', Validators.required],
            fromAssetId: ['', Validators.required],
            toAssetId: ['', Validators.required],
            amountSpent: [null, [Validators.required, Validators.min(0)]],
            amountReceived: [null, [Validators.required, Validators.min(0)]],
            spotPriceInUsd: [null, Validators.required],
            spotPriceInEur: [null],
            fee: [0],
            feeAssetId: [''],
            feeSpotPriceInUsd: [null],
            feeSpotPriceInEur: [null],
            notes: ['']
        });
    }

    ngOnInit() {
        this.portfolioService.getTransactionTypes()
            .pipe(finalize(() => this.isLoadingTypes.set(false)))
            .subscribe({
                next: (types) => {
                    this.transactionTypes.set(types);
                    if (types.length > 0) {
                        const firstType = types[0].value;
                        this.form.get('type')?.setValue(firstType);
                        this.updateValidators(types[0]);
                    }
                },
                error: (err) => {
                    console.error('Failed to load transaction types', err);
                }
            });

        // Listen to form type changes to update validators
        this.form.get('type')?.valueChanges.subscribe(typeValue => {
            const types = this.transactionTypes();
            const typeData = types.find(t => t.value === typeValue);
            if (typeData) {
                this.updateValidators(typeData);
            }
        });
    }

    private updateValidators(typeData: TransactionType) {
        const fromControl = this.form.get('fromAssetId');
        const toControl = this.form.get('toAssetId');
        const spentControl = this.form.get('amountSpent');
        const receivedControl = this.form.get('amountReceived');

        // Reset validators
        fromControl?.clearValidators();
        toControl?.clearValidators();
        spentControl?.clearValidators();
        receivedControl?.clearValidators();

        if (typeData.requiresFromAsset) {
            fromControl?.setValidators(Validators.required);
            spentControl?.setValidators([Validators.required, Validators.min(0)]);
        } else {
            spentControl?.setValue(0);
            fromControl?.setValue('');
        }

        if (typeData.requiresToAsset) {
            toControl?.setValidators(Validators.required);
            receivedControl?.setValidators([Validators.required, Validators.min(0)]);
        } else {
            receivedControl?.setValue(0);
            toControl?.setValue('');
        }

        fromControl?.updateValueAndValidity();
        toControl?.updateValueAndValidity();
        spentControl?.updateValueAndValidity();
        receivedControl?.updateValueAndValidity();
    }

    onSubmit() {
        if (this.form.invalid || this.isSubmitting()) return;

        this.isSubmitting.set(true);
        const formValue = this.form.value;

        const request: NewTransactionRequest = {
            date: formValue.date.toISOString(),
            transactionTypeCode: formValue.type,
            fromAssetId: formValue.fromAssetId || undefined,
            toAssetId: formValue.toAssetId || undefined,
            amountSpent: Number(formValue.amountSpent || 0),
            amountReceived: Number(formValue.amountReceived || 0),
            spotPriceInUsd: Number(formValue.spotPriceInUsd),
            spotPriceInEur: formValue.spotPriceInEur ? Number(formValue.spotPriceInEur) : undefined,
            fee: Number(formValue.fee || 0),
            feeAssetId: formValue.feeAssetId || undefined,
            feeSpotPriceInUsd: formValue.feeSpotPriceInUsd ? Number(formValue.feeSpotPriceInUsd) : undefined,
            feeSpotPriceInEur: formValue.feeSpotPriceInEur ? Number(formValue.feeSpotPriceInEur) : undefined,
            notes: formValue.notes
        };

        this.portfolioService.addTransaction(request)
            .pipe(finalize(() => this.isSubmitting.set(false)))
            .subscribe({
                next: () => {
                    this.router.navigate(['/']); // Back to Dashboard
                },
                error: (err) => {
                    console.error('Failed to save transaction', err);
                }
            });
    }

    onCancel() {
        this.router.navigate(['/']);
    }
}
