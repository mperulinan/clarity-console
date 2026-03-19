import { Component, OnInit, signal, computed, ChangeDetectionStrategy, WritableSignal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators, AbstractControl } from '@angular/forms';
import { Router } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { debounceTime, switchMap, catchError, of, startWith, filter } from 'rxjs';

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

const UI_CONFIG: Record<string, any> = {
    DEPOSIT: { toTitle: 'Asset Deposited', toIcon: 'south_east', toAmount: 'Amount Deposited' },
    WITHDRAWAL: { fromTitle: 'Asset Withdrawn', fromIcon: 'north_east', fromAmount: 'Amount Withdrawn' },
    SWAP: { fromTitle: 'Asset Sold', fromIcon: 'sell', fromAmount: 'Amount Sold', toTitle: 'Asset Bought', toIcon: 'shopping_cart', toAmount: 'Amount Bought' },
    REWARD: { toTitle: 'Asset Rewarded', toIcon: 'workspace_premium', toAmount: 'Reward Amount' },
    DEFAULT: { fromTitle: 'Disposed Asset', fromIcon: 'transit_enterexit', fromAmount: 'Total Amount Spent', toTitle: 'Acquired Asset', toIcon: 'account_balance_wallet', toAmount: 'Total Amount Received' },
};

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

    // Selected asset state for chip display
    selectedFromAsset = signal<AssetDto | null>(null);
    selectedToAsset = signal<AssetDto | null>(null);
    selectedFeeAsset = signal<AssetDto | null>(null);

    private readonly assetSignalMap: Record<string, WritableSignal<AssetDto | null>> = {};

    fiatCurrencies = signal<AssetDto[]>([]);

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
                amountReceived: [null, [Validators.required, Validators.min(0)]],
                spotPrice: [null, [Validators.required, Validators.min(0)]],
                spotPriceCurrency: ['USD', Validators.required]
            }),
            step2: this.fb.group({
                fee: [0],
                feeAssetId: [''],
                feeSpotPrice: [null, Validators.min(0)],
                feeSpotPriceCurrency: ['USD']
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
            if (!typeData) {
                return UI_CONFIG['DEFAULT'];
            }

            return UI_CONFIG[typeData.value.toUpperCase()] || UI_CONFIG['DEFAULT'];
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
                    this.form.get(controlPath)?.setValue(syncedAsset);
                    this.assetSignalMap[controlPath]?.set(syncedAsset);
                },
                error: (err) => {
                    console.error('Failed to sync asset', err);
                    this.form.get(controlPath)?.setValue(null);
                    this.assetSignalMap[controlPath]?.set(null);
                }
            });
        } else {
            this.assetSignalMap[controlPath]?.set(asset);
        }
    }

    clearAsset(controlPath: string) {
        this.form.get(controlPath)?.setValue(null);
        this.assetSignalMap[controlPath]?.set(null);
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

        // Fetch Fiat Currencies
        this.portfolioService.getFiatCurrencies().subscribe({
            next: (fiats) => {
                this.fiatCurrencies.set(fiats);
            },
            error: (err) => {
                console.error('Failed to load fiat currencies', err);
            }
        });

        // Setup Autocomplete pipelines
        this.filteredFromAssets = signal<AssetDto[]>([]);
        this.filteredToAssets = signal<AssetDto[]>([]);
        this.filteredFeeAssets = signal<AssetDto[]>([]);

        // Register signal map for chip management
        (this.assetSignalMap as any)['step1.fromAssetId'] = this.selectedFromAsset;
        (this.assetSignalMap as any)['step1.toAssetId'] = this.selectedToAsset;
        (this.assetSignalMap as any)['step2.feeAssetId'] = this.selectedFeeAsset;

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

        // Listen for Fiat Asset Selections to Auto-lock pricing
        this.form.get('step1')?.valueChanges.subscribe(step1Value => {
            this.updateReactiveLocks(step1Value);
        });

        this.form.get('step2.feeAssetId')?.valueChanges.subscribe(feeAsset => {
            this.updateFeeLocks(feeAsset);
        });
    }

    private updateReactiveLocks(step1Value: any) {
        const spotPriceControl = this.form.get('step1.spotPrice');
        const spotPriceCurrencyControl = this.form.get('step1.spotPriceCurrency');

        const fromAsset = step1Value.fromAssetId;
        const toAsset = step1Value.toAssetId;

        // Dynamically check if either selected asset matches a known fiat currency
        const matchedFiat =
            this.fiatCurrencies().find(f => f.id === fromAsset?.id) ??
            this.fiatCurrencies().find(f => f.id === toAsset?.id);

        if (matchedFiat) {
            spotPriceControl?.setValue(1, { emitEvent: false });
            spotPriceControl?.disable({ emitEvent: false });
            spotPriceCurrencyControl?.setValue(matchedFiat.symbol);
            spotPriceCurrencyControl?.disable({ emitEvent: false });
        } else {
            if (spotPriceControl?.disabled) {
                spotPriceControl?.enable({ emitEvent: false });
                spotPriceControl?.setValue(null, { emitEvent: false });
                spotPriceCurrencyControl?.enable({ emitEvent: false });
            }
        }
    }

    private updateFeeLocks(feeAsset: any) {
        const feePriceControl = this.form.get('step2.feeSpotPrice');
        const feePriceCurrencyControl = this.form.get('step2.feeSpotPriceCurrency');

        // Dynamically check if the fee asset matches a known fiat currency
        const matchedFiat = this.fiatCurrencies().find(f => f.id === feeAsset?.id);

        if (matchedFiat) {
            feePriceControl?.setValue(1, { emitEvent: false });
            feePriceControl?.disable({ emitEvent: false });
            feePriceCurrencyControl?.setValue(matchedFiat.symbol);
            feePriceCurrencyControl?.disable({ emitEvent: false });
        } else {
            if (feePriceControl?.disabled) {
                feePriceControl?.enable({ emitEvent: false });
                feePriceControl?.setValue(null, { emitEvent: false });
                feePriceCurrencyControl?.enable({ emitEvent: false });
            }
        }
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
        const step1Value = this.form.get('step1')?.getRawValue();
        // getRawValue gets disabled control values too
        const step2Value = this.form.get('step2')?.getRawValue();
        const step3Value = this.form.get('step3')?.value;

        const request: NewTransactionRequest = {
            date: new Date(step1Value.date).toISOString(),
            transactionTypeCode: step1Value.type,
            fromAssetId: step1Value.fromAssetId?.id || undefined,
            toAssetId: step1Value.toAssetId?.id || undefined,
            amountSpent: Number(step1Value.amountSpent || 0),
            amountReceived: Number(step1Value.amountReceived || 0),
            spotPriceInUsd: step1Value.spotPriceCurrency === 'USD' ? Number(step1Value.spotPrice) : undefined,
            spotPriceInEur: step1Value.spotPriceCurrency === 'EUR' ? Number(step1Value.spotPrice) : undefined,
            spotPriceInputCurrency: step1Value.spotPriceCurrency,
            fee: Number(step2Value.fee || 0),
            feeAssetId: step2Value.feeAssetId?.id || undefined,
            feeSpotPriceInUsd: step2Value.feeSpotPriceCurrency === 'USD' && step2Value.feeSpotPrice ? Number(step2Value.feeSpotPrice) : undefined,
            feeSpotPriceInEur: step2Value.feeSpotPriceCurrency === 'EUR' && step2Value.feeSpotPrice ? Number(step2Value.feeSpotPrice) : undefined,
            feePriceInputCurrency: step2Value.feeSpotPrice && step2Value.feeSpotPriceCurrency ? step2Value.feeSpotPriceCurrency : undefined,
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

    get step1Value(): any {
        return this.step1Group?.getRawValue() || {};
    }

    get step2Value(): any {
        return this.step2Group?.getRawValue() || {};
    }

    get isStep1Valid(): boolean {
        return this.form.get('step1')?.valid ?? false;
    }

    get isStep2Valid(): boolean {
        return this.form.get('step2')?.valid ?? false;
    }
}
