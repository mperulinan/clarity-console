import { Component, forwardRef, Input, ViewEncapsulation, SimpleChanges, OnChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR, ReactiveFormsModule, FormControl } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';

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
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => InteractivePriceInputComponent),
            multi: true
        }
    ],
    encapsulation: ViewEncapsulation.None
})
export class InteractivePriceInputComponent implements ControlValueAccessor, OnChanges {
    @Input() assetSymbol: string = '';
    @Input() assetAmount: number | null = 0;
    @Input() fiatCurrency: string = DEFAULT_FIAT_CURRENCY;

    mode: PriceInputMode = PriceInputMode.Unit;
    readonly PriceInputMode = PriceInputMode;
    internalControl = new FormControl<number | null>(null);

    // CVA methods
    onChange: any = () => { };
    onTouched: any = () => { };

    private currentUnitPrice: number | null = null;

    get currencyPrefix(): string {
        return FIAT_CURRENCY_SYMBOLS[this.fiatCurrency as keyof typeof FIAT_CURRENCY_SYMBOLS] || this.fiatCurrency;
    }

    get safeAmount(): number {
        return this.assetAmount && this.assetAmount > 0 ? this.assetAmount : 0;
    }

    get calculatedTotal(): number | null {
        if (this.currentUnitPrice === null) return null;
        return this.currentUnitPrice * this.safeAmount;
    }

    get calculatedUnit(): number | null {
        return this.currentUnitPrice;
    }

    constructor() {
        this.internalControl.valueChanges.subscribe(val => {
            if (val === null || val < 0) {
                this.currentUnitPrice = null;
            } else if (this.mode === PriceInputMode.Unit) {
                this.currentUnitPrice = val;
            } else if (this.mode === PriceInputMode.Total) {
                if (this.safeAmount > 0) {
                    this.currentUnitPrice = val / this.safeAmount;
                } else {
                    this.currentUnitPrice = null;
                }
            }
            this.onChange(this.currentUnitPrice);
        });
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['assetAmount']) {
            // If the amount changes and we are in TOTAL mode, we need to recalculate the unit price
            // without changing the total value string input
            if (this.mode === PriceInputMode.Total && this.internalControl.value !== null) {
                if (this.safeAmount > 0) {
                    this.currentUnitPrice = this.internalControl.value / this.safeAmount;
                } else {
                    this.currentUnitPrice = null;
                }
                this.onChange(this.currentUnitPrice);
            }
        }
    }

    toggleMode() {
        this.mode = this.mode === PriceInputMode.Unit ? PriceInputMode.Total : PriceInputMode.Unit;
        
        // Refresh the UI input based on the new mode
        if (this.currentUnitPrice !== null) {
            if (this.mode === PriceInputMode.Unit) {
                this.internalControl.setValue(this.currentUnitPrice, { emitEvent: false });
            } else {
                this.internalControl.setValue(this.currentUnitPrice * this.safeAmount, { emitEvent: false });
            }
        } else {
            this.internalControl.setValue(null, { emitEvent: false });
        }
    }

    writeValue(value: number | null): void {
        this.currentUnitPrice = value;
        if (value !== null) {
            if (this.mode === PriceInputMode.Unit) {
                this.internalControl.setValue(value, { emitEvent: false });
            } else {
                this.internalControl.setValue(value * this.safeAmount, { emitEvent: false });
            }
        } else {
            this.internalControl.setValue(null, { emitEvent: false });
        }
    }

    registerOnChange(fn: any): void {
        this.onChange = fn;
    }

    registerOnTouched(fn: any): void {
        this.onTouched = fn;
    }

    setDisabledState(isDisabled: boolean): void {
        if (isDisabled) {
            this.internalControl.disable({ emitEvent: false });
        } else {
            this.internalControl.enable({ emitEvent: false });
        }
    }
}
