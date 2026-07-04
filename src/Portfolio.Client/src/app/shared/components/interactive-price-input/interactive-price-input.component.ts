import {
    Component, input, output, signal, computed, effect, untracked,
    ViewEncapsulation, ChangeDetectionStrategy, inject
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormControl } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { CurrencyService } from '../../../services/currency.service';

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
    readonly fiatCurrency = input<string>('');
    /** Current unit price driven by the parent. */
    readonly value = input<number | null>(null);
    /** Whether the input should be non-interactive. */
    readonly disabled = input<boolean>(false);

    // ── Outputs ──────────────────────────────────────────────────────────
    /** Emits the derived unit price whenever it changes. */
    readonly valueChange = output<number | null>();

    private readonly currencyService = inject(CurrencyService);
    readonly PriceInputMode = PriceInputMode;

    mode = signal<PriceInputMode>(PriceInputMode.Unit);
    currentUnitPrice = signal<number | null>(null);
    /** Text-based control — locale-safe, always parsed with dot as decimal separator. */
    internalControl = new FormControl<string | null>(null);

    currencyPrefix = computed(() => {
        const code = this.fiatCurrency() || this.currencyService.defaultCurrency()?.symbol || 'USD';
        try {
            return Intl.NumberFormat('en-US', { style: 'currency', currency: code })
                .formatToParts(0)
                .find(p => p.type === 'currency')?.value || code;
        } catch {
            return code;
        }
    });

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
                    this.internalControl.setValue(this.formatNumeric(displayValue), { emitEvent: false });
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
                    const inputVal = this.parseNumericInput(this.internalControl.value);
                    const newUnit = (amount > 0 && inputVal !== null) ? inputVal / amount : null;
                    this.currentUnitPrice.set(newUnit);
                    this.valueChange.emit(newUnit);
                }
            });
        });

        // Drive currentUnitPrice and notify parent from user input
        this.internalControl.valueChanges
            .pipe(takeUntilDestroyed())
            .subscribe(raw => {
                const val = this.parseNumericInput(raw);
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
            this.internalControl.setValue(this.formatNumeric(displayValue), { emitEvent: false });
        } else {
            this.internalControl.setValue(null, { emitEvent: false });
        }
    }

    /**
     * Accepts both dot and comma as decimal separator and always returns a JS number
     * (which internally uses dot). Rejects values that cannot be parsed.
     */
    private parseNumericInput(raw: string | null | undefined): number | null {
        if (raw === null || raw === undefined || raw.toString().trim() === '') return null;
        // Normalise: replace comma decimal separator with dot
        const normalised = raw.toString().trim().replace(',', '.');
        const parsed = parseFloat(normalised);
        return isNaN(parsed) ? null : parsed;
    }

    /**
     * Formats a JS number to a plain decimal string without locale-specific separators.
     * Strips unnecessary trailing zeros.
     */
    private formatNumeric(value: number): string {
        // toFixed(8) then trim trailing zeros and possible trailing dot
        return parseFloat(value.toFixed(8)).toString();
    }
}
