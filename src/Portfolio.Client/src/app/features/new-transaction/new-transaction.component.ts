import { Component, OnInit, signal, computed, ChangeDetectionStrategy, WritableSignal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators, AbstractControl } from '@angular/forms';
import { Router } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { Observable, debounceTime, switchMap, catchError, of, map, startWith, filter, tap } from 'rxjs';

// Material
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatStepperModule } from '@angular/material/stepper';
import { MatAutocompleteModule, MatAutocompleteSelectedEvent } from '@angular/material/autocomplete';
import { MatOptionModule } from '@angular/material/core';

import { PortfolioService, AssetDto } from '../../services/portfolio.service';
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
        MatButtonToggleModule,
        MatStepperModule,
        MatAutocompleteModule,
        MatOptionModule
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
    isSyncingAsset = signal<boolean>(false);
    transactionTypes = signal<TransactionType[]>([]);

    filteredFromAssets!: WritableSignal<AssetDto[]>;
    filteredToAssets!: WritableSignal<AssetDto[]>;
    filteredFeeAssets!: WritableSignal<AssetDto[]>;

    // Reactive Form to Signal Bridge
    private typeValueChange: () => string;

    // Computed state
    selectedTypeData: () => TransactionType | undefined;
    uiLabels: () => any;

    constructor(
        private fb: FormBuilder,
        private portfolioService: PortfolioService,
        private router: Router
    ) {
        this.form = this.fb.group({
            step1: this.fb.group({
                // Format to YYYY-MM-DDThh:mm for datetime-local
                date: [new Date().toISOString().slice(0, 16), Validators.required],
                type: ['', Validators.required],
                fromAssetId: ['', Validators.required],
                toAssetId: ['', Validators.required],
                amountSpent: [null, [Validators.required, Validators.min(0)]],
                amountReceived: [null, [Validators.required, Validators.min(0)]]
            }),
            step2: this.fb.group({
                spotPriceInUsd: [null, Validators.required],
                spotPriceInEur: [null],
                fee: [0],
                feeAssetId: [''],
                feeSpotPriceInUsd: [null],
                feeSpotPriceInEur: [null]
            }),
            step3: this.fb.group({
                notes: ['']
            })
        });

        // Initialize signals that depend on form
        this.typeValueChange = toSignal(this.form.get('step1.type')!.valueChanges, { initialValue: '' });

        this.selectedTypeData = computed(() => {
            const typeValue = this.typeValueChange();
            const types = this.transactionTypes();
            return types.find(t => t.value === typeValue);
        });

        this.uiLabels = computed(() => {
            const typeData = this.selectedTypeData();
            if (!typeData) return {
                fromTitle: 'Disposed Asset', fromIcon: 'transit_enterexit', fromAmount: 'Total Amount Spent',
                toTitle: 'Acquired Asset', toIcon: 'account_balance_wallet', toAmount: 'Total Amount Received'
            };

            switch (typeData.value.toUpperCase()) {
                case 'DEPOSIT':
                    return {
                        toTitle: 'Asset Deposited', toIcon: 'south_east', toAmount: 'Amount Deposited',
                        fromTitle: '', fromIcon: '', fromAmount: ''
                    };
                case 'WITHDRAWAL':
                    return {
                        fromTitle: 'Asset Withdrawn', fromIcon: 'north_east', fromAmount: 'Amount Withdrawn',
                        toTitle: '', toIcon: '', toAmount: ''
                    };
                case 'SWAP':
                    return {
                        fromTitle: 'Asset Sold', fromIcon: 'sell', fromAmount: 'Amount Sold',
                        toTitle: 'Asset Bought', toIcon: 'shopping_cart', toAmount: 'Amount Bought'
                    };
                case 'REWARD':
                    return {
                        toTitle: 'Asset Rewarded', toIcon: 'workspace_premium', toAmount: 'Reward Amount',
                        fromTitle: '', fromIcon: '', fromAmount: ''
                    };
                default:
                    return {
                        fromTitle: 'Disposed Asset', fromIcon: 'transit_enterexit', fromAmount: 'Total Amount Spent',
                        toTitle: 'Acquired Asset', toIcon: 'account_balance_wallet', toAmount: 'Total Amount Received'
                    };
            }
        });
    }

    private setupAutocomplete(controlPath: string, targetSignal: WritableSignal<AssetDto[]>) {
        this.form.get(controlPath)?.valueChanges.pipe(
            startWith(''),
            filter(value => typeof value === 'string'),
            debounceTime(300),
            switchMap((value: string) => {
                if (!value || value.length < 2) return of([]);
                return this.portfolioService.searchAssets(value).pipe(
                    catchError(() => of([]))
                );
            })
        ).subscribe(assets => targetSignal.set(assets));
    }

    displayAssetFn(asset: AssetDto): string {
        return asset ? `${asset.name} (${asset.symbol})` : '';
    }

    onAssetSelected(event: MatAutocompleteSelectedEvent, controlPath: string) {
        const asset = event.option.value as AssetDto;
        if (!asset) return;

        if (!asset.id || asset.id === '00000000-0000-0000-0000-000000000000') {
            // Need to sync external asset first
            this.isSyncingAsset.set(true);
            this.form.get(controlPath)?.disable();

            this.portfolioService.syncAsset(asset).pipe(
                finalize(() => {
                    this.isSyncingAsset.set(false);
                    this.form.get(controlPath)?.enable();
                })
            ).subscribe({
                next: (syncedAsset) => {
                    this.form.get(controlPath)?.setValue(syncedAsset, { emitEvent: false });
                },
                error: (err) => {
                    console.error('Failed to sync asset', err);
                    this.form.get(controlPath)?.setValue(null); // Clear on fail
                }
            });
        }
    }

    onAssetInputBlur(controlPath: string) {
        // Enforce strict object selection (if value is still a string, they didn't pick an option)
        const control = this.form.get(controlPath);
        if (control && typeof control.value === 'string') {
            control.setValue(null);
        }
    }

    ngOnInit() {
        this.portfolioService.getTransactionTypes()
            .pipe(finalize(() => this.isLoadingTypes.set(false)))
            .subscribe({
                next: (types) => {
                    this.transactionTypes.set(types);
                    if (types.length > 0) {
                        const firstType = types[0].value;
                        this.form.get('step1.type')?.setValue(firstType);
                        this.updateValidators(types[0]);
                    }
                },
                error: (err) => {
                    console.error('Failed to load transaction types', err);
                }
            });

        // Setup Autocomplete pipelines
        this.filteredFromAssets = signal<AssetDto[]>([]);
        this.filteredToAssets = signal<AssetDto[]>([]);
        this.filteredFeeAssets = signal<AssetDto[]>([]);

        this.setupAutocomplete('step1.fromAssetId', this.filteredFromAssets);
        this.setupAutocomplete('step1.toAssetId', this.filteredToAssets);
        this.setupAutocomplete('step2.feeAssetId', this.filteredFeeAssets);

        // Listen to form type changes to update validators
        this.form.get('step1.type')?.valueChanges.subscribe(typeValue => {
            const types = this.transactionTypes();
            const typeData = types.find(t => t.value === typeValue);
            if (typeData) {
                this.updateValidators(typeData);
            }
        });
    }

    private updateValidators(typeData: TransactionType) {
        const step1 = this.form.get('step1');
        const fromControl = step1?.get('fromAssetId');
        const toControl = step1?.get('toAssetId');
        const spentControl = step1?.get('amountSpent');
        const receivedControl = step1?.get('amountReceived');

        // Reset validators
        fromControl?.clearValidators();
        toControl?.clearValidators();
        spentControl?.clearValidators();
        receivedControl?.clearValidators();

        if (typeData.requiresFromAsset) {
            fromControl?.setValidators([Validators.required, this.requireAssetObject]);
            spentControl?.setValidators([Validators.required, Validators.min(0)]);
        } else {
            spentControl?.setValue(0);
            fromControl?.setValue('');
        }

        if (typeData.requiresToAsset) {
            toControl?.setValidators([Validators.required, this.requireAssetObject]);
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

    private requireAssetObject(control: AbstractControl): { [key: string]: boolean } | null {
        if (!control.value) return null;
        return typeof control.value === 'string' ? { 'requireMatch': true } : null;
    }

    onSubmit() {
        if (this.form.invalid || this.isSubmitting()) return;

        this.isSubmitting.set(true);
        const step1Value = this.form.get('step1')?.value;
        const step2Value = this.form.get('step2')?.value;
        const step3Value = this.form.get('step3')?.value;

        const request: NewTransactionRequest = {
            date: new Date(step1Value.date).toISOString(),
            transactionTypeCode: step1Value.type,
            fromAssetId: step1Value.fromAssetId?.id || undefined,
            toAssetId: step1Value.toAssetId?.id || undefined,
            amountSpent: Number(step1Value.amountSpent || 0),
            amountReceived: Number(step1Value.amountReceived || 0),
            spotPriceInUsd: Number(step2Value.spotPriceInUsd),
            spotPriceInEur: step2Value.spotPriceInEur ? Number(step2Value.spotPriceInEur) : undefined,
            fee: Number(step2Value.fee || 0),
            feeAssetId: step2Value.feeAssetId?.id || undefined,
            feeSpotPriceInUsd: step2Value.feeSpotPriceInUsd ? Number(step2Value.feeSpotPriceInUsd) : undefined,
            feeSpotPriceInEur: step2Value.feeSpotPriceInEur ? Number(step2Value.feeSpotPriceInEur) : undefined,
            notes: step3Value.notes
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

    get step1Group() {
        return this.form.get('step1');
    }

    get step2Group() {
        return this.form.get('step2');
    }

    get isStep1Valid(): boolean {
        return this.form.get('step1')?.valid ?? false;
    }

    get isStep2Valid(): boolean {
        return this.form.get('step2')?.valid ?? false;
    }
}
