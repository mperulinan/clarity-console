import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { forkJoin, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { AssetDto } from '../models/asset';

@Injectable({
    providedIn: 'root'
})
export class CurrencyService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = environment.apiUrl;

    /** The supported fiat currencies loaded from the backend. */
    readonly supportedCurrencies = signal<AssetDto[]>([]);

    /** The tax currency loaded from the backend. */
    readonly taxCurrency = signal<AssetDto | null>(null);

    /** The default display currency loaded from the backend. */
    readonly defaultCurrency = signal<AssetDto | null>(null);

    /**
     * Initializes the currency configuration by fetching the supported 
     * currencies, tax currency, and default currency from the backend.
     * This is intended to be called by an APP_INITIALIZER.
     */
    initialize() {
        return forkJoin({
            supportedCurrencies: this.http.get<AssetDto[]>(`${this.apiUrl}/Asset/fiat-currencies`),
            taxCurrency: this.http.get<AssetDto>(`${this.apiUrl}/Asset/tax-currency`),
            defaultCurrency: this.http.get<AssetDto>(`${this.apiUrl}/Asset/default-currency`)
        }).pipe(
            tap(({ supportedCurrencies, taxCurrency, defaultCurrency }) => {
                this.supportedCurrencies.set(supportedCurrencies);
                this.taxCurrency.set(taxCurrency);
                this.defaultCurrency.set(defaultCurrency);
            })
        );
    }
}
