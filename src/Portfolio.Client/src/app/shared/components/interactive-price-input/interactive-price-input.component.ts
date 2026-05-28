import {
    Component, input, output, signal, computed, effect, untracked,
    ViewEncapsulation, ChangeDetectionStrategy
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormControl } from '@angular/forms';
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
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class InteractivePriceInputComponent {
    // ── Inputs ───────────────────────────────────────────────────────────
    readonly assetSymbol = input<string>('');
    readonly assetAmount = input<number | null>(0);
    readonly fiatCurrency = input<string>(DEFAULT_FIAT_CURRENCY);
    /** Current unit price driven by the parent. */
    readonly value = input<number | null>(null);
    /** Whether the input should be non-interactive. */
    readonly disabled = input<boolean>(false);
    /** Warning state */
    readonly showWarning = input<boolean>(false);
    readonly warningMessage = input<string>('');

    // ── Outputs ──────────────────────────────────────────────────────────
    /** Emits the derived unit price whenever it changes. */
    readonly valueChange = output<number | null>();

    readonly PriceInputMode = PriceInputMode;

    mode = signal<PriceInputMode>(PriceInputMode.Unit);
    private currentUnitPrice = signal<number | null>(null);
    internalControl = new FormControl<number | null>(null);

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
        // Sync value input → internal control (only when value changes externally)
        effect(() => {
            const v = this.value();
            untracked(() => {
                this.currentUnitPrice.set(v);
                if (v !== null) {
                    const displayValue = this.mode() === PriceInputMode.Unit
                        ? v
                        : v * this.safeAmount();
                    this.internalControl.setValue(displayValue, { emitEvent: false });
                } else {
                    this.internalControl.setValue(null, { emitEvent: false });
                }
            });
        });

        // Sync disabled input → internal control enabled state
        effect(() => {
            if (this.disabled()) {
                this.internalControl.disable({ emitEvent: false });
            } else if (this.internalControl.disabled) {
                this.internalControl.enable({ emitEvent: false });
            }
        });

        // Replaces ngOnChanges: react to assetAmount changes while in Total mode
        effect(() => {
            const amount = this.safeAmount();
            untracked(() => {
                if (this.mode() === PriceInputMode.Total && this.internalControl.value !== null) {
                    const newUnit = amount > 0 ? this.internalControl.value / amount : null;
                    this.currentUnitPrice.set(newUnit);
                    this.valueChange.emit(newUnit);
                }
            });
        });

        // Drive currentUnitPrice and notify parent from user input
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
                this.valueChange.emit(this.currentUnitPrice());
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
}
