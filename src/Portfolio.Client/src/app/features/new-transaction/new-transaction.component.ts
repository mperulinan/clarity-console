import { Component, OnInit, signal, computed, ChangeDetectionStrategy, WritableSignal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormGroup, FormControl, Validators, AbstractControl } from '@angular/forms';
import { Router } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';

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
import { PortfolioService, AssetDto } from '../../services/portfolio.service';
import { NewTransactionRequest } from '../../models/new-transaction-request';
import { TransactionType } from '../../models/transaction';

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
    SWAP: { fromTitle: 'Asset Sold', fromIcon: 'sell', fromAmount: 'Amount Sold', toTitle: 'Asset Bought', toIcon: 'shopping_cart', toAmount: 'Amount Bought' },
    REWARD: { toTitle: 'Asset Rewarded', toIcon: 'workspace_premium', toAmount: 'Amount Rewarded' },
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
        AssetSelectorComponent
    ],
    templateUrl: './new-transaction.component.html',
    styleUrl: './new-transaction.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class NewTransactionComponent implements OnInit {
    form: FormGroup<TransactionForm>;

    // State using Signals
    isSubmitting = signal<boolean>(false);
    isLoadingTypes = signal<boolean>(true);
    transactionTypes = signal<TransactionType[]>([]);

    fiatCurrencies = signal<AssetDto[]>([]);

    // Reactive Form to Signal Bridge
    private typeValueChange: () => string;

    // Computed state
    selectedTypeData: () => TransactionType | undefined;
    uiLabels: () => any;

    constructor(
        private portfolioService: PortfolioService,
        private router: Router
    ) {
        this.form = new FormGroup<TransactionForm>({
            step1: new FormGroup<Step1Form>({
                // Format to YYYY-MM-DDThh:mm for datetime-local
                date: new FormControl<string>(new Date().toISOString().slice(0, 16), { nonNullable: true, validators: Validators.required }),
                type: new FormControl<string>('', { nonNullable: true, validators: Validators.required }),
                fromAssetId: new FormControl<AssetDto | null>(null, Validators.required),
                toAssetId: new FormControl<AssetDto | null>(null, Validators.required),
                amountSpent: new FormControl<number | null>(null, [Validators.required, this.requireGreaterThanZero]),
                amountReceived: new FormControl<number | null>(null, [Validators.required, this.requireGreaterThanZero])
            }),
            step2: new FormGroup<Step2Form>({
                spotPrice: new FormControl<number | null>(null, [Validators.required, this.requireGreaterThanZero]),
                spotPriceCurrency: new FormControl<string>('USD', { nonNullable: true, validators: Validators.required }),
                fee: new FormControl<number | null>(0),
                feeAssetId: new FormControl<AssetDto | null>(null),
                feeSpotPrice: new FormControl<number | null>(null, Validators.min(0)),
                feeSpotPriceCurrency: new FormControl<string>('USD', { nonNullable: true })
            }),
            step3: new FormGroup<Step3Form>({
                notes: new FormControl<string | null>(null)
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

    ngOnInit() {
        this.portfolioService.getTransactionTypes()
            .pipe(finalize(() => this.isLoadingTypes.set(false)))
            .subscribe({
                next: (types) => {
                    this.transactionTypes.set(types);
                    if (types.length > 0) {
                        const firstType = types[0].value;
                        this.form.controls.step1.controls.type.setValue(firstType);
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

        // Wire up auto-clearing for Spot Prices when Asset controls change
        this.form.controls.step1.controls.fromAssetId.valueChanges.subscribe(() => {
            this.form.controls.step2.controls.spotPrice.setValue(null);
        });

        this.form.controls.step1.controls.toAssetId.valueChanges.subscribe(() => {
            this.form.controls.step2.controls.spotPrice.setValue(null);
        });

        this.form.controls.step2.controls.feeAssetId.valueChanges.subscribe(() => {
            this.form.controls.step2.controls.feeSpotPrice.setValue(null);
        });

        // Listen to form type changes to update validators
        this.form.controls.step1.controls.type.valueChanges.subscribe(typeValue => {
            const types = this.transactionTypes();
            const typeData = types.find(t => t.value === typeValue);
            if (typeData) {
                this.migrateValuesOnTypeChange(typeData);
                this.updateValidators(typeData);
            }
        });

        // Listen for Fiat Asset Selections to Auto-lock pricing
        this.form.controls.step1.valueChanges.subscribe(step1Value => {
            this.updateReactiveLocks(step1Value);
        });

        this.form.controls.step2.valueChanges.subscribe(() => {
            this.updateFeeLocks();
        });
    }

    private updateReactiveLocks(step1Value: any) {
        const spotPriceControl = this.form.controls.step2.controls.spotPrice;
        const spotPriceCurrencyControl = this.form.controls.step2.controls.spotPriceCurrency;

        if (this.hasFiatLeg) {
            spotPriceControl.disable({ emitEvent: false });
            spotPriceCurrencyControl.disable({ emitEvent: false });
        } else {
            if (spotPriceControl.disabled) {
                spotPriceControl.enable({ emitEvent: false });
                spotPriceCurrencyControl.enable({ emitEvent: false });
            }
        }
    }

    private updateFeeLocks() {
        const feePriceControl = this.form.controls.step2.controls.feeSpotPrice;
        const feePriceCurrencyControl = this.form.controls.step2.controls.feeSpotPriceCurrency;

        if (!this.showFeeFiatValuation) {
            feePriceControl.disable({ emitEvent: false });
            feePriceCurrencyControl.disable({ emitEvent: false });
            feePriceControl.setValue(null, { emitEvent: false });
        } else {
            if (feePriceControl.disabled) {
                feePriceControl.enable({ emitEvent: false });
                feePriceCurrencyControl.enable({ emitEvent: false });
            }
        }
    }

    private migrateValuesOnTypeChange(newType: TransactionType) {
        const step1 = this.form.controls.step1;
        const fromAsset = step1.controls.fromAssetId.value;
        const toAsset = step1.controls.toAssetId.value;
        const fromAmount = step1.controls.amountSpent.value;
        const toAmount = step1.controls.amountReceived.value;

        // If new type ONLY needs FROM, and FROM is empty, and TO has a value
        if (newType.requiresFromAsset && !newType.requiresToAsset && !fromAsset && toAsset) {
            step1.controls.fromAssetId.setValue(toAsset);
            if (!fromAmount && toAmount && toAmount > 0) {
                step1.controls.amountSpent.setValue(toAmount);
            }
        }
        // If new type ONLY needs TO, and TO is empty, and FROM has a value
        else if (newType.requiresToAsset && !newType.requiresFromAsset && !toAsset && fromAsset) {
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

        // Reset validators
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

    onSubmit() {
        if (this.form.invalid || this.isSubmitting()) return;

        this.isSubmitting.set(true);
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
            spotPriceInUsd: step2Value.spotPriceCurrency === 'USD' ? Number(step2Value.spotPrice) : undefined,
            spotPriceInEur: step2Value.spotPriceCurrency === 'EUR' ? Number(step2Value.spotPrice) : undefined,
            spotPriceInputCurrency: step2Value.spotPriceCurrency,
            fee: Number(step2Value.fee || 0),
            feeAssetId: typeof step2Value.feeAssetId === 'object' ? step2Value.feeAssetId?.id : undefined,
            feeSpotPriceInUsd: step2Value.feeSpotPriceCurrency === 'USD' && step2Value.feeSpotPrice ? Number(step2Value.feeSpotPrice) : undefined,
            feeSpotPriceInEur: step2Value.feeSpotPriceCurrency === 'EUR' && step2Value.feeSpotPrice ? Number(step2Value.feeSpotPrice) : undefined,
            feePriceInputCurrency: step2Value.feeSpotPrice && step2Value.feeSpotPriceCurrency ? step2Value.feeSpotPriceCurrency : undefined,
            notes: step3Value.notes ?? undefined
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
        return this.form.controls.step1;
    }

    get step2Group() {
        return this.form.controls.step2;
    }

    get step1Value(): any {
        return this.step1Group.getRawValue();
    }

    get step2Value(): any {
        return this.step2Group.getRawValue();
    }

    get pricedAssetSymbol(): string {
        let defaultSymbol = 'Coin';
        const typeData = this.selectedTypeData();
        if (!typeData) return defaultSymbol;

        // Swap (both true) and Withdrawal (from true) price the From Asset
        if (typeData.requiresFromAsset) {
            return this.step1Value?.fromAssetId?.symbol || defaultSymbol;
        }

        // Deposit and Reward price the To Asset
        return this.step1Value?.toAssetId?.symbol || defaultSymbol;
    }

    get feeAssetSymbol(): string {
        return this.step2Value?.feeAssetId?.symbol || 'Coin';
    }

    get hasFiatLeg(): boolean {
        const fromAsset = this.step1Value?.fromAssetId;
        const toAsset = this.step1Value?.toAssetId;
        return this.fiatCurrencies().some(f => f.id === fromAsset?.id || f.id === toAsset?.id);
    }

    get hasFiatFee(): boolean {
        const feeAsset = this.step2Value?.feeAssetId;
        return this.fiatCurrencies().some(f => f.id === feeAsset?.id);
    }

    get showFeeFiatValuation(): boolean {
        const feeAmount = this.step2Value?.fee || 0;
        const hasAsset = !!this.step2Value?.feeAssetId;
        return feeAmount > 0 && hasAsset && !this.hasFiatFee;
    }

    get isStep1Valid(): boolean {
        return this.step1Group.valid;
    }

    get isStep2Valid(): boolean {
        return this.step2Group.valid;
    }
}
