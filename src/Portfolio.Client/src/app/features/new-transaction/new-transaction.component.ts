import {
    Component, OnInit, signal, computed,
    effect, untracked, inject, ChangeDetectionStrategy
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';

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

// Signal Forms
import {
    FormField, form, submit,
    required, validate, disabled
} from '@angular/forms/signals';

// Shared components
import { AssetSelectorComponent } from '../../shared/components/asset-selector/asset-selector.component';
import { InteractivePriceInputComponent } from '../../shared/components/interactive-price-input/interactive-price-input.component';
import { ButtonComponent } from '../../shared/components/button/button.component';
import { BadgeComponent } from '../../shared/components/badge/badge.component';
import { SkeletonComponent } from '../../shared/components/skeleton/skeleton.component';

// Services & models
import { PortfolioService } from '../../services/portfolio.service';
import { AssetDto } from '../../models/asset';
import { NewTransactionRequest } from '../../models/new-transaction-request';
import { TransactionType } from '../../models/transaction';
import { DEFAULT_FIAT_CURRENCY, SupportedFiatCurrency } from '../../shared/constants/currency.constants';

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
        FormField,
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
        InteractivePriceInputComponent,
        ButtonComponent,
        BadgeComponent,
        SkeletonComponent
    ],
    templateUrl: './new-transaction.component.html',
    styleUrl: './new-transaction.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class NewTransactionComponent implements OnInit {
    private readonly portfolioService = inject(PortfolioService);
    private readonly router = inject(Router);

    isSubmitting = signal<boolean>(false);
    submitError = signal<string | null>(null);
    isLoadingTypes = signal<boolean>(true);
    transactionTypes = signal<TransactionType[]>([]);
    fiatCurrencies = signal<AssetDto[]>([]);

    // ── Form model — flat, no nulls (Signal Forms requirement) ───────────
    model = signal({
        date: new Date().toISOString().slice(0, 16),
        type: '',
        amountSpent: 0,
        amountReceived: 0,
        spotPrice: 0,
        spotPriceCurrency: DEFAULT_FIAT_CURRENCY as SupportedFiatCurrency,
        fee: 0,
        feeSpotPrice: 0,
        feeSpotPriceCurrency: DEFAULT_FIAT_CURRENCY as SupportedFiatCurrency,
        notes: '',
    });

    // ── Asset objects managed alongside the form (objects cannot be in the signal form model) ──
    fromAsset = signal<AssetDto | null>(null);
    toAsset = signal<AssetDto | null>(null);
    feeAsset = signal<AssetDto | null>(null);

    // ── Computed signals (same logic as before, now reading model() directly) ──
    selectedTypeData = computed(() => {
        const typeValue = this.model().type;
        return this.transactionTypes().find(t => t.value === typeValue);
    });

    uiLabels = computed(() => {
        const typeData = this.selectedTypeData();
        if (!typeData) return UI_CONFIG['DEFAULT'];
        return UI_CONFIG[typeData.value.toUpperCase()] || UI_CONFIG['DEFAULT'];
    });

    hasFiatLeg = computed(() => {
        return this.fiatCurrencies().some(
            f => f.id === this.fromAsset()?.id || f.id === this.toAsset()?.id
        );
    });

    hasFiatFee = computed(() =>
        this.fiatCurrencies().some(f => f.id === this.feeAsset()?.id)
    );

    showFeeFiatValuation = computed(() => !!this.feeAsset() && !this.hasFiatFee());

    isFeeAssetSameAsSpotAsset = computed(() => {
        const fee = this.feeAsset();
        if (!fee) return false;
        const spot = this.fromAsset() ?? this.toAsset();
        return !!spot && fee.id === spot.id;
    });

    pricedAssetSymbol = computed(() => {
        const typeData = this.selectedTypeData();
        if (!typeData) return 'Coin';
        return typeData.requiresFromAsset
            ? (this.fromAsset()?.symbol ?? 'Coin')
            : (this.toAsset()?.symbol ?? 'Coin');
    });

    pricedAssetAmount = computed(() => {
        const typeData = this.selectedTypeData();
        if (!typeData) return 0;
        return typeData.requiresFromAsset
            ? this.model().amountSpent
            : this.model().amountReceived;
    });

    feeAssetSymbol = computed(() => this.feeAsset()?.symbol ?? 'Coin');

    // ── Step validity (replaces [stepControl] on MatStepper) ────────────
    isStep1Valid = computed(() => {
        const typeData = this.selectedTypeData();
        if (!typeData || !this.model().date) return false;
        if (typeData.requiresFromAsset && (!this.fromAsset() || this.model().amountSpent <= 0)) return false;
        if (typeData.requiresToAsset && (!this.toAsset() || this.model().amountReceived <= 0)) return false;
        return true;
    });

    isStep2Valid = computed(() => {
        const hasFeeAsset = !!this.feeAsset();
        const feeAmount = this.model().fee;
        // If one side of fee is filled, both are required
        if (hasFeeAsset !== (feeAmount > 0)) return false;
        // spotPrice required unless fiat leg
        if (!this.hasFiatLeg() && this.model().spotPrice <= 0) return false;
        // feeSpotPrice required when fee exists and fee asset is not fiat and not same as spot
        if (this.showFeeFiatValuation() && !this.isFeeAssetSameAsSpotAsset() && this.model().feeSpotPrice <= 0) return false;
        return true;
    });

    // ── Effective fee spot price (mirrored from spot when same asset) ────
    effectiveFeeSpotPrice = computed(() =>
        this.isFeeAssetSameAsSpotAsset() ? this.model().spotPrice : this.model().feeSpotPrice
    );

    effectiveFeeSpotPriceCurrency = computed(() =>
        this.isFeeAssetSameAsSpotAsset() ? this.model().spotPriceCurrency : this.model().feeSpotPriceCurrency
    );

    // ── Signal Form ──────────────────────────────────────────────────────
    transactionForm = form(this.model, s => {
        required(s.type, { message: 'Transaction type is required' });
        required(s.date, { message: 'Date is required' });

        // Amounts — conditionally required based on type
        validate(s.amountSpent, ({ value, valueOf }) => {
            const typeValue = valueOf(s.type);
            const typeData = this.transactionTypes().find(t => t.value === typeValue);
            if (!typeData?.requiresFromAsset) return undefined;
            if (!this.fromAsset()) return { kind: 'required', message: 'From asset is required' };
            if (!value() || value() <= 0) return { kind: 'minExclusive', message: 'Amount must be greater than 0' };
            return undefined;
        });

        validate(s.amountReceived, ({ value, valueOf }) => {
            const typeValue = valueOf(s.type);
            const typeData = this.transactionTypes().find(t => t.value === typeValue);
            if (!typeData?.requiresToAsset) return undefined;
            if (!this.toAsset()) return { kind: 'required', message: 'To asset is required' };
            if (!value() || value() <= 0) return { kind: 'minExclusive', message: 'Amount must be greater than 0' };
            return undefined;
        });

        validate(s.spotPrice, ({ value }) => {
            if (this.hasFiatLeg()) return undefined;
            if (!value() || value() <= 0) return { kind: 'required', message: 'Spot price is required' };
            return undefined;
        });

        validate(s.fee, ({ value }) => {
            if (!!this.feeAsset() && (!value() || value() <= 0))
                return { kind: 'minExclusive', message: 'Fee amount must be greater than 0' };
            return undefined;
        });

        validate(s.feeSpotPrice, ({ value }) => {
            if (!this.showFeeFiatValuation() || this.isFeeAssetSameAsSpotAsset()) return undefined;
            if (!value() || value() <= 0) return { kind: 'required', message: 'Fee spot price is required' };
            return undefined;
        });

        // Disable spotPrice fields when there is a fiat leg
        disabled(s.spotPrice, () => this.hasFiatLeg());
        disabled(s.spotPriceCurrency, () => this.hasFiatLeg());

        // Disable feeSpotPrice fields when not applicable
        disabled(s.feeSpotPrice, () => !this.showFeeFiatValuation() || this.isFeeAssetSameAsSpotAsset());
        disabled(s.feeSpotPriceCurrency, () => !this.showFeeFiatValuation() || this.isFeeAssetSameAsSpotAsset());
    });

    constructor() {
        // Effect: clear spotPrice when the spot asset changes
        effect(() => {
            this.fromAsset();
            untracked(() => this.model.update(m => ({ ...m, spotPrice: 0 })));
        });
        effect(() => {
            this.toAsset();
            untracked(() => this.model.update(m => ({ ...m, spotPrice: 0 })));
        });
        // Effect: clear feeSpotPrice when fee asset changes
        effect(() => {
            this.feeAsset();
            untracked(() => this.model.update(m => ({ ...m, feeSpotPrice: 0 })));
        });

        // Effect: carry over asset selection when transaction type changes
        effect(() => {
            const typeValue = this.model().type;
            const typeData = this.transactionTypes().find(t => t.value === typeValue);
            if (!typeData) return;
            untracked(() => {
                if (typeData.requiresFromAsset && !typeData.requiresToAsset && !this.fromAsset() && this.toAsset()) {
                    this.fromAsset.set(this.toAsset());
                    this.model.update(m => ({ ...m, amountSpent: m.amountReceived > 0 ? m.amountReceived : m.amountSpent }));
                } else if (typeData.requiresToAsset && !typeData.requiresFromAsset && !this.toAsset() && this.fromAsset()) {
                    this.toAsset.set(this.fromAsset());
                    this.model.update(m => ({ ...m, amountReceived: m.amountSpent > 0 ? m.amountSpent : m.amountReceived }));
                }
            });
        });
    }

    ngOnInit() {
        this.portfolioService.getTransactionTypes().subscribe({
            next: types => {
                this.transactionTypes.set(types);
                this.isLoadingTypes.set(false);
                if (types.length > 0) {
                    this.model.update(m => ({ ...m, type: types[0].value }));
                }
            },
            error: err => {
                console.error('Failed to load transaction types', err);
                this.isLoadingTypes.set(false);
            }
        });

        this.portfolioService.getFiatCurrencies().subscribe({
            next: fiats => this.fiatCurrencies.set(fiats),
            error: err => console.error('Failed to load fiat currencies', err)
        });
    }

    // ── Model update helpers (used by template event bindings) ──────────

    onTypeChange(value: string) {
        this.model.update(m => ({ ...m, type: value }));
    }

    onSpotCurrencyChange(value: string) {
        this.model.update(m => ({ ...m, spotPriceCurrency: value as SupportedFiatCurrency }));
    }

    onSpotPriceChange(value: number | null) {
        this.model.update(m => ({ ...m, spotPrice: value ?? 0 }));
    }

    onFeeChange(value: number) {
        this.model.update(m => ({ ...m, fee: value }));
    }

    onFeeCurrencyChange(value: string) {
        this.model.update(m => ({ ...m, feeSpotPriceCurrency: value as SupportedFiatCurrency }));
    }

    onFeeSpotPriceChange(value: number | null) {
        this.model.update(m => ({ ...m, feeSpotPrice: value ?? 0 }));
    }

    // ── Submit / Cancel ──────────────────────────────────────────────────

    onSubmit() {
        submit(this.transactionForm, async () => {
            this.isSubmitting.set(true);
            this.submitError.set(null);

            const m = this.model();
            const request: NewTransactionRequest = {
                date: new Date(m.date).toISOString(),
                transactionTypeCode: m.type,
                fromAssetId: this.fromAsset()?.id,
                toAssetId: this.toAsset()?.id,
                amountSpent: m.amountSpent,
                amountReceived: m.amountReceived,
                spotPriceUSD: m.spotPriceCurrency === 'USD' ? m.spotPrice : undefined,
                spotPriceEUR: m.spotPriceCurrency === 'EUR' ? m.spotPrice : undefined,
                spotPriceInputCurrency: m.spotPriceCurrency,
                fee: this.feeAsset()?.id ? m.fee : 0,
                feeAssetId: this.feeAsset()?.id,
                feePriceUSD: this.effectiveFeeSpotPriceCurrency() === 'USD' && this.effectiveFeeSpotPrice()
                    ? this.effectiveFeeSpotPrice() : undefined,
                feePriceEUR: this.effectiveFeeSpotPriceCurrency() === 'EUR' && this.effectiveFeeSpotPrice()
                    ? this.effectiveFeeSpotPrice() : undefined,
                feePriceInputCurrency: this.effectiveFeeSpotPrice() && this.effectiveFeeSpotPriceCurrency()
                    ? this.effectiveFeeSpotPriceCurrency() : undefined,
                notes: m.notes || undefined
            };

            try {
                await firstValueFrom(this.portfolioService.addTransaction(request));
                this.router.navigate(['/']);
            } catch (err) {
                console.error('Failed to save transaction', err);
                this.submitError.set('Failed to save transaction. Please check your connection and try again.');
            } finally {
                this.isSubmitting.set(false);
            }
        });
    }

    onCancel() {
        this.router.navigate(['/']);
    }
}
