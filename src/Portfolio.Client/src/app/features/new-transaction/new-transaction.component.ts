import {
    Component, OnInit, signal, computed, Signal,
    effect, untracked, inject, DestroyRef, ChangeDetectionStrategy
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormGroup, FormControl, Validators, AbstractControl } from '@angular/forms';
import { Router } from '@angular/router';
import { toSignal, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize, map } from 'rxjs';

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
import { AssetSelectorComponent } from '../../shared/components/asset-selector/asset-selector.component';
import { InteractivePriceInputComponent } from '../../shared/components/interactive-price-input/interactive-price-input.component';
import { PortfolioService, AssetDto } from '../../services/portfolio.service';
import { NewTransactionRequest } from '../../models/new-transaction-request';
import { TransactionType } from '../../models/transaction';
import { DEFAULT_FIAT_CURRENCY } from '../../shared/constants/currency.constants';

export interface Step1Form {
    date: FormControl<string>;
    type: FormControl<string>;
    fromAssetId: FormControl<AssetDto | null>;
    toAssetId: FormControl<AssetDto | null>;
    amountSpent: FormControl<number | null>;
    amountReceived: FormControl<number | null>;
}

export interface Step2Form {
    spotPrice: FormControl<number | null>;
    spotPriceCurrency: FormControl<string>;
    fee: FormControl<number | null>;
    feeAssetId: FormControl<AssetDto | null>;
    feeSpotPrice: FormControl<number | null>;
    feeSpotPriceCurrency: FormControl<string>;
}

export interface Step3Form {
    notes: FormControl<string | null>;
}

export interface TransactionForm {
    step1: FormGroup<Step1Form>;
    step2: FormGroup<Step2Form>;
    step3: FormGroup<Step3Form>;
}

const UI_CONFIG: Record<string, any> = {
    DEPOSIT: { toTitle: 'Asset Deposited', toIcon: 'south_east', toAmount: 'Amount Deposited' },
    WITHDRAWAL: { fromTitle: 'Asset Withdrawn', fromIcon: 'north_east', fromAmount: 'Amount Withdrawn' },
    SWAP: {
        fromTitle: 'Asset Sold', fromIcon: 'sell', fromAmount: 'Amount Sold',
        toTitle: 'Asset Bought', toIcon: 'shopping_cart', toAmount: 'Amount Bought'
    },
    REWARD: { toTitle: 'Asset Rewarded', toIcon: 'workspace_premium', toAmount: 'Amount Rewarded' },
    DEFAULT: {
        fromTitle: 'Disposed Asset', fromIcon: 'transit_enterexit', fromAmount: 'Total Amount Spent',
        toTitle: 'Acquired Asset', toIcon: 'account_balance_wallet', toAmount: 'Total Amount Received'
    },
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
        AssetSelectorComponent,
        InteractivePriceInputComponent
    ],
    templateUrl: './new-transaction.component.html',
    styleUrl: './new-transaction.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class NewTransactionComponent implements OnInit {
    form: FormGroup<TransactionForm>;

    // ── Writable state signals ───────────────────────────────────────────
    isSubmitting = signal<boolean>(false);
    submitError = signal<string | null>(null);
    isLoadingTypes = signal<boolean>(true);
    transactionTypes = signal<TransactionType[]>([]);
    fiatCurrencies = signal<AssetDto[]>([]);

    // ── Form-value signals (initialized in constructor after form is built)
    private step1Raw!: Signal<any>;
    private step2Raw!: Signal<any>;
    private typeValueChange!: Signal<string>;

    // ── Computed signals ─────────────────────────────────────────────────
    selectedTypeData!: Signal<TransactionType | undefined>;
    uiLabels!: Signal<any>;
    hasFiatLeg!: Signal<boolean>;
    hasFiatFee!: Signal<boolean>;
    showFeeFiatValuation!: Signal<boolean>;
    isFeeAssetSameAsSpotAsset!: Signal<boolean>;
    pricedAssetSymbol!: Signal<string>;
    pricedAssetAmount!: Signal<number>;
    feeAssetSymbol!: Signal<string>;

    private readonly destroyRef = inject(DestroyRef);

    constructor(
        private portfolioService: PortfolioService,
        private router: Router
    ) {
        this.form = new FormGroup<TransactionForm>({
            step1: new FormGroup<Step1Form>({
                date: new FormControl<string>(new Date().toISOString().slice(0, 16), { nonNullable: true, validators: Validators.required }),
                type: new FormControl<string>('', { nonNullable: true, validators: Validators.required }),
                fromAssetId: new FormControl<AssetDto | null>(null, Validators.required),
                toAssetId: new FormControl<AssetDto | null>(null, Validators.required),
                amountSpent: new FormControl<number | null>(null, [Validators.required, this.requireGreaterThanZero]),
                amountReceived: new FormControl<number | null>(null, [Validators.required, this.requireGreaterThanZero])
            }),
            step2: new FormGroup<Step2Form>({
                spotPrice: new FormControl<number | null>(null, [Validators.required, this.requireGreaterThanZero]),
                spotPriceCurrency: new FormControl<string>(DEFAULT_FIAT_CURRENCY, { nonNullable: true, validators: Validators.required }),
                fee: new FormControl<number | null>(0),
                feeAssetId: new FormControl<AssetDto | null>(null),
                feeSpotPrice: new FormControl<number | null>(null, Validators.min(0)),
                feeSpotPriceCurrency: new FormControl<string>(DEFAULT_FIAT_CURRENCY, { nonNullable: true })
            }),
            step3: new FormGroup<Step3Form>({
                notes: new FormControl<string | null>(null)
            })
        });

        // ── Signal bridges: form value changes → signals ─────────────────
        // pipe(map(() => getRawValue())) ensures disabled controls are included
        this.step1Raw = toSignal(
            this.form.controls.step1.valueChanges.pipe(map(() => this.form.controls.step1.getRawValue())),
            { initialValue: this.form.controls.step1.getRawValue() }
        );

        this.step2Raw = toSignal(
            this.form.controls.step2.valueChanges.pipe(map(() => this.form.controls.step2.getRawValue())),
            { initialValue: this.form.controls.step2.getRawValue() }
        );

        this.typeValueChange = toSignal(
            this.form.controls.step1.controls.type.valueChanges,
            { initialValue: '' }
        );

        // ── Computed signals ─────────────────────────────────────────────
        this.selectedTypeData = computed(() => {
            const typeValue = this.typeValueChange();
            return this.transactionTypes().find(t => t.value === typeValue);
        });

        this.uiLabels = computed(() => {
            const typeData = this.selectedTypeData();
            if (!typeData) {
                return UI_CONFIG['DEFAULT'];
            }

            return UI_CONFIG[typeData.value.toUpperCase()] || UI_CONFIG['DEFAULT'];
        });

        this.hasFiatLeg = computed(() => {
            const { fromAssetId, toAssetId } = this.step1Raw();
            return this.fiatCurrencies().some(f => f.id === fromAssetId?.id || f.id === toAssetId?.id);
        });

        this.hasFiatFee = computed(() => {
            const { feeAssetId } = this.step2Raw();
            return this.fiatCurrencies().some(f => f.id === feeAssetId?.id);
        });

        this.showFeeFiatValuation = computed(() => {
            const { feeAssetId } = this.step2Raw();
            return !!feeAssetId && !this.hasFiatFee();
        });

        this.isFeeAssetSameAsSpotAsset = computed(() => {
            const { feeAssetId } = this.step2Raw();
            if (!feeAssetId) return false;
            const { fromAssetId, toAssetId } = this.step1Raw();
            const spotAsset = fromAssetId || toAssetId;
            return !!spotAsset && feeAssetId.id === spotAsset.id;
        });

        this.pricedAssetSymbol = computed(() => {
            const typeData = this.selectedTypeData();
            if (!typeData) return 'Coin';
            const { fromAssetId, toAssetId } = this.step1Raw();
            return typeData.requiresFromAsset
                ? (fromAssetId?.symbol || 'Coin')
                : (toAssetId?.symbol || 'Coin');
        });

        this.pricedAssetAmount = computed(() => {
            const typeData = this.selectedTypeData();
            if (!typeData) return 0;
            const { amountSpent, amountReceived } = this.step1Raw();
            return typeData.requiresFromAsset ? (amountSpent || 0) : (amountReceived || 0);
        });

        this.feeAssetSymbol = computed(() => {
            const { feeAssetId } = this.step2Raw();
            return feeAssetId?.symbol || 'Coin';
        });

        // ── Effects: replace form.valueChanges subscriptions ────────────
        // hasFiatLeg depends on step1Raw + fiatCurrencies — effect tracks both
        effect(() => {
            this.hasFiatLeg();
            untracked(() => this.updateReactiveLocks());
        });

        // Fee locks depend on everything that can change fee price state
        effect(() => {
            this.showFeeFiatValuation();      // tracks step2Raw + hasFiatFee
            this.isFeeAssetSameAsSpotAsset(); // tracks step1Raw + step2Raw
            this.step2Raw();                  // also tracks raw step2 for spotPrice & fee amount
            untracked(() => this.updateFeeLocks());
        });
    }

    ngOnInit() {
        this.portfolioService.getTransactionTypes()
            .pipe(finalize(() => this.isLoadingTypes.set(false)))
            .subscribe({
                next: (types) => {
                    this.transactionTypes.set(types);
                    if (types.length > 0) {
                        this.form.controls.step1.controls.type.setValue(types[0].value);
                        this.updateValidators(types[0]);
                    }
                },
                error: (err) => console.error('Failed to load transaction types', err)
            });

        this.portfolioService.getFiatCurrencies().subscribe({
            next: (fiats) => this.fiatCurrencies.set(fiats),
            error: (err) => console.error('Failed to load fiat currencies', err)
        });

        // Clear spot price when the spot asset changes
        this.form.controls.step1.controls.fromAssetId.valueChanges
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(() => this.form.controls.step2.controls.spotPrice.setValue(null));

        this.form.controls.step1.controls.toAssetId.valueChanges
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(() => this.form.controls.step2.controls.spotPrice.setValue(null));

        // Clear fee spot price when the fee asset changes
        this.form.controls.step2.controls.feeAssetId.valueChanges
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(() => this.form.controls.step2.controls.feeSpotPrice.setValue(null));

        // Type change: migrate carried values + refresh validators
        this.form.controls.step1.controls.type.valueChanges
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(typeValue => {
                const typeData = this.transactionTypes().find(t => t.value === typeValue);
                if (typeData) {
                    this.migrateValuesOnTypeChange(typeData);
                    this.updateValidators(typeData);
                }
            });
    }

    // ── Private: form control side-effects ──────────────────────────────

    private updateReactiveLocks() {
        const spotPriceControl = this.form.controls.step2.controls.spotPrice;
        const spotPriceCurrencyControl = this.form.controls.step2.controls.spotPriceCurrency;

        if (this.hasFiatLeg()) {
            spotPriceControl.disable({ emitEvent: false });
            spotPriceCurrencyControl.disable({ emitEvent: false });
        } else if (spotPriceControl.disabled) {
            spotPriceControl.enable({ emitEvent: false });
            spotPriceCurrencyControl.enable({ emitEvent: false });
        }
    }

    private updateFeeLocks() {
        const feeAssetControl = this.form.controls.step2.controls.feeAssetId;
        const feeControl = this.form.controls.step2.controls.fee;
        const feePriceControl = this.form.controls.step2.controls.feeSpotPrice;
        const feePriceCurrencyControl = this.form.controls.step2.controls.feeSpotPriceCurrency;

        const hasFeeAsset = !!feeAssetControl.value;
        const hasFeeAmount = !!feeControl.value && feeControl.value > 0;

        // Ensure strict symmetry: fee amount needs an asset and vice-versa
        if (hasFeeAsset || hasFeeAmount) {
            feeControl.setValidators([Validators.required, this.requireGreaterThanZero]);
            feeAssetControl.setValidators([Validators.required, this.requireAssetObject]);
        } else {
            feeControl.clearValidators();
            feeAssetControl.clearValidators();
        }
        feeControl.updateValueAndValidity({ emitEvent: false });
        feeAssetControl.updateValueAndValidity({ emitEvent: false });

        if (!this.showFeeFiatValuation()) {
            // No fee, or fee asset is fiat — no crypto price input needed
            if (this.hasFiatFee() && feeAssetControl.value?.symbol) {
                feePriceCurrencyControl.setValue(feeAssetControl.value.symbol, { emitEvent: false });
            }
            feePriceControl.disable({ emitEvent: false });
            feePriceCurrencyControl.disable({ emitEvent: false });
            feePriceControl.clearValidators();
            feePriceCurrencyControl.clearValidators();
            feePriceControl.setValue(null, { emitEvent: false });
        } else if (this.isFeeAssetSameAsSpotAsset()) {
            // Mirror spot price — same asset, no separate price needed
            const spotPrice = this.form.controls.step2.controls.spotPrice.value;
            const spotCurrency = this.form.controls.step2.controls.spotPriceCurrency.value;
            feePriceControl.setValue(spotPrice, { emitEvent: false });
            feePriceCurrencyControl.setValue(spotCurrency, { emitEvent: false });
            feePriceControl.disable({ emitEvent: false });
            feePriceCurrencyControl.disable({ emitEvent: false });
            feePriceControl.clearValidators();
            feePriceCurrencyControl.clearValidators();
        } else {
            if (feePriceControl.disabled) {
                feePriceControl.enable({ emitEvent: false });
                feePriceCurrencyControl.enable({ emitEvent: false });
            }
            feePriceControl.setValidators([Validators.required, this.requireGreaterThanZero]);
            feePriceCurrencyControl.setValidators([Validators.required]);
        }
        feePriceControl.updateValueAndValidity({ emitEvent: false });
        feePriceCurrencyControl.updateValueAndValidity({ emitEvent: false });
    }

    private migrateValuesOnTypeChange(newType: TransactionType) {
        const step1 = this.form.controls.step1;
        const fromAsset = step1.controls.fromAssetId.value;
        const toAsset = step1.controls.toAssetId.value;
        const fromAmount = step1.controls.amountSpent.value;
        const toAmount = step1.controls.amountReceived.value;

        if (newType.requiresFromAsset && !newType.requiresToAsset && !fromAsset && toAsset) {
            step1.controls.fromAssetId.setValue(toAsset);
            if (!fromAmount && toAmount && toAmount > 0) {
                step1.controls.amountSpent.setValue(toAmount);
            }
        } else if (newType.requiresToAsset && !newType.requiresFromAsset && !toAsset && fromAsset) {
            step1.controls.toAssetId.setValue(fromAsset);
            if (!toAmount && fromAmount && fromAmount > 0) {
                step1.controls.amountReceived.setValue(fromAmount);
            }
        }
    }

    private updateValidators(typeData: TransactionType) {
        const step1 = this.form.controls.step1;
        const fromControl = step1.controls.fromAssetId;
        const toControl = step1.controls.toAssetId;
        const spentControl = step1.controls.amountSpent;
        const receivedControl = step1.controls.amountReceived;

        fromControl.clearValidators();
        toControl.clearValidators();
        spentControl.clearValidators();
        receivedControl.clearValidators();

        if (typeData.requiresFromAsset) {
            fromControl.enable();
            spentControl.enable();
            fromControl.setValidators([Validators.required, this.requireAssetObject]);
            spentControl.setValidators([Validators.required, this.requireGreaterThanZero]);
        } else {
            spentControl.setValue(0);
            fromControl.setValue(null);
            spentControl.disable();
            fromControl.disable();
        }

        if (typeData.requiresToAsset) {
            toControl.enable();
            receivedControl.enable();
            toControl.setValidators([Validators.required, this.requireAssetObject]);
            receivedControl.setValidators([Validators.required, this.requireGreaterThanZero]);
        } else {
            receivedControl.setValue(0);
            toControl.setValue(null);
            receivedControl.disable();
            toControl.disable();
        }

        fromControl.updateValueAndValidity();
        toControl.updateValueAndValidity();
        spentControl.updateValueAndValidity();
        receivedControl.updateValueAndValidity();
    }

    private requireAssetObject(control: AbstractControl): { [key: string]: boolean } | null {
        if (!control.value) return null;
        return typeof control.value === 'string' ? { 'requireMatch': true } : null;
    }

    private requireGreaterThanZero(control: AbstractControl): { [key: string]: boolean } | null {
        if (control.value === null || control.value === undefined || control.value === '') return null;
        return Number(control.value) > 0 ? null : { 'minExclusive': true };
    }

    // ── Submit / Cancel ──────────────────────────────────────────────────

    onSubmit() {
        if (this.form.invalid || this.isSubmitting()) return;

        this.isSubmitting.set(true);
        this.submitError.set(null);
        const step1Value = this.form.controls.step1.getRawValue();
        const step2Value = this.form.controls.step2.getRawValue();
        const step3Value = this.form.controls.step3.getRawValue();

        const request: NewTransactionRequest = {
            date: new Date(step1Value.date).toISOString(),
            transactionTypeCode: step1Value.type,
            fromAssetId: typeof step1Value.fromAssetId === 'object' ? step1Value.fromAssetId?.id : undefined,
            toAssetId: typeof step1Value.toAssetId === 'object' ? step1Value.toAssetId?.id : undefined,
            amountSpent: Number(step1Value.amountSpent || 0),
            amountReceived: Number(step1Value.amountReceived || 0),
            spotPriceUSD: step2Value.spotPriceCurrency === 'USD' ? Number(step2Value.spotPrice) : undefined,
            spotPriceEUR: step2Value.spotPriceCurrency === 'EUR' ? Number(step2Value.spotPrice) : undefined,
            spotPriceInputCurrency: step2Value.spotPriceCurrency,
            fee: (step2Value.feeAssetId && step2Value.feeAssetId.id) ? Number(step2Value.fee || 0) : 0,
            feeAssetId: typeof step2Value.feeAssetId === 'object' ? step2Value.feeAssetId?.id : undefined,
            feePriceUSD: step2Value.feeSpotPriceCurrency === 'USD' && step2Value.feeSpotPrice ? Number(step2Value.feeSpotPrice) : undefined,
            feePriceEUR: step2Value.feeSpotPriceCurrency === 'EUR' && step2Value.feeSpotPrice ? Number(step2Value.feeSpotPrice) : undefined,
            feePriceInputCurrency: step2Value.feeSpotPrice && step2Value.feeSpotPriceCurrency ? step2Value.feeSpotPriceCurrency : undefined,
            notes: step3Value.notes ?? undefined
        };

        this.portfolioService.addTransaction(request)
            .pipe(finalize(() => this.isSubmitting.set(false)))
            .subscribe({
                next: () => this.router.navigate(['/']),
                error: (err) => {
                    console.error('Failed to save transaction', err);
                    this.submitError.set('Failed to save transaction. Please check your connection and try again.');
                }
            });
    }

    onCancel() {
        this.router.navigate(['/']);
    }

    // ── Accessors ────────────────────────────────────────────────────────

    get step1Group() { return this.form.controls.step1; }
    get step2Group() { return this.form.controls.step2; }

    /** Template reads go through the signal — Angular tracks the dependency correctly. */
    get step1Value(): any { return this.step1Raw(); }
    get step2Value(): any { return this.step2Raw(); }

    get isStep1Valid(): boolean { return this.step1Group.valid; }
    get isStep2Valid(): boolean { return this.step2Group.valid; }
}
