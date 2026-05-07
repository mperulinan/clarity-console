import {
    Component, forwardRef, input, signal, computed, effect, untracked,
    ViewEncapsulation, ChangeDetectionStrategy
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR, ReactiveFormsModule, FormControl } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { FIAT_CURRENCY_SYMBOLS, DEFAULT_FIAT_CURRENCY } from '../../constants/currency.constants';

export enum PriceInputMode {
    Unit = 'UNIT',
    Total = 'TOTAL'
}

@Component({
    selector: 'app-interactive-price-input',
    standalone: true,
    imports: [
        CommonModule,
        ReactiveFormsModule,
        MatFormFieldModule,
        MatInputModule,
        MatIconModule,
        MatButtonModule,
        MatTooltipModule
    ],
    templateUrl: './interactive-price-input.component.html',
    styleUrl: './interactive-price-input.component.scss',
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => InteractivePriceInputComponent),
            multi: true
        }
    ],
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class InteractivePriceInputComponent implements ControlValueAccessor {
    readonly assetSymbol = input<string>('');
    readonly assetAmount = input<number | null>(0);
    readonly fiatCurrency = input<string>(DEFAULT_FIAT_CURRENCY);

    readonly PriceInputMode = PriceInputMode;

    mode = signal<PriceInputMode>(PriceInputMode.Unit);
    private currentUnitPrice = signal<number | null>(null);
    internalControl = new FormControl<number | null>(null);

    onChange: (v: number | null) => void = () => { };
    onTouched: () => void = () => { };

    currencyPrefix = computed(() =>
        FIAT_CURRENCY_SYMBOLS[this.fiatCurrency() as keyof typeof FIAT_CURRENCY_SYMBOLS] || this.fiatCurrency()
    );

    safeAmount = computed(() => {
        const amt = this.assetAmount();
        return amt && amt > 0 ? amt : 0;
    });

    calculatedTotal = computed(() => {
        const price = this.currentUnitPrice();
        if (price === null) return null;
        return price * this.safeAmount();
    });

    calculatedUnit = computed(() => this.currentUnitPrice());

    constructor() {
        // Replaces ngOnChanges: react to assetAmount changes while in Total mode
        effect(() => {
            const amount = this.safeAmount();
            untracked(() => {
                if (this.mode() === PriceInputMode.Total && this.internalControl.value !== null) {
                    const newUnit = amount > 0 ? this.internalControl.value / amount : null;
                    this.currentUnitPrice.set(newUnit);
                    this.onChange(newUnit);
                }
            });
        });

        // Drive currentUnitPrice and notify CVA from user input
        this.internalControl.valueChanges
            .pipe(takeUntilDestroyed())
            .subscribe(val => {
                if (val === null || val < 0) {
                    this.currentUnitPrice.set(null);
                } else if (this.mode() === PriceInputMode.Unit) {
                    this.currentUnitPrice.set(val);
                } else {
                    const amt = this.safeAmount();
                    this.currentUnitPrice.set(amt > 0 ? val / amt : null);
                }
                this.onChange(this.currentUnitPrice());
            });
    }

    toggleMode(): void {
        this.mode.update(m => m === PriceInputMode.Unit ? PriceInputMode.Total : PriceInputMode.Unit);

        const price = this.currentUnitPrice();
        if (price !== null) {
            const displayValue = this.mode() === PriceInputMode.Unit
                ? price
                : price * this.safeAmount();
            this.internalControl.setValue(displayValue, { emitEvent: false });
        } else {
            this.internalControl.setValue(null, { emitEvent: false });
        }
    }

    // ── ControlValueAccessor ─────────────────────────────────────────────
    writeValue(value: number | null): void {
        this.currentUnitPrice.set(value);
        if (value !== null) {
            const displayValue = this.mode() === PriceInputMode.Unit
                ? value
                : value * this.safeAmount();
            this.internalControl.setValue(displayValue, { emitEvent: false });
        } else {
            this.internalControl.setValue(null, { emitEvent: false });
        }
    }

    registerOnChange(fn: (v: number | null) => void): void { this.onChange = fn; }
    registerOnTouched(fn: () => void): void { this.onTouched = fn; }

    setDisabledState(isDisabled: boolean): void {
        isDisabled
            ? this.internalControl.disable({ emitEvent: false })
            : this.internalControl.enable({ emitEvent: false });
    }
}
