import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, forkJoin, map, shareReplay } from 'rxjs';
import { PortfolioMetrics } from '../models/portfolio-metrics';
import { PortfolioReport } from '../models/portfolio-report';
import { NewTransactionRequest } from '../models/new-transaction-request';
import { environment } from '../../environments/environment';
import { TransactionType, ProcessedTransaction, Transaction } from '../models/transaction';
import { AssetDto } from '../models/asset';

@Injectable({
    providedIn: 'root'
})
export class PortfolioService {
    private apiUrl = environment.apiUrl;

    private readonly http = inject(HttpClient);

    private transactionTypes$ = this.http.get<TransactionType[]>(`${this.apiUrl}/Transaction/types`).pipe(
        shareReplay(1)
    );

    getPortfolioReport(): Observable<PortfolioReport> {
        return forkJoin({
            report: this.http.get<PortfolioReport>(`${this.apiUrl}/Portfolio/tax-report`),
            types: this.getTransactionTypes()
        }).pipe(
            map(({ report, types }) => {
                const typesMap = new Map<string, TransactionType>();
                types.forEach(t => typesMap.set(t.value, t));

                return {
                    ...report,
                    reportingCurrency: report.reportingCurrency,
                    transactions: report.transactions.map(pt => this.normalizeProcessedTransaction(pt, typesMap)),
                    holdings: report.holdings,
                    yearSummaries: (report.yearSummaries || []).map(ys => ({
                        ...ys,
                        totalGains: Number(ys.totalGains),
                        totalLosses: Number(ys.totalLosses),
                        netPL: Number(ys.netPL),
                        disallowedLosses: Number(ys.disallowedLosses),
                        year: Number(ys.year)
                    }))
                };
            })
        );
    }

    getPortfolioMetrics(): Observable<PortfolioMetrics> {
        return this.http.get<PortfolioMetrics>(`${this.apiUrl}/Portfolio/metrics`);
    }

    addTransaction(request: NewTransactionRequest): Observable<void> {
        return this.http.post<void>(`${this.apiUrl}/Transaction`, request);
    }

    getTransactionTypes(): Observable<TransactionType[]> {
        return this.transactionTypes$;
    }

    searchAssets(query: string, type?: string): Observable<AssetDto[]> {
        let params = new HttpParams().set('query', query);
        if (type) {
            params = params.set('type', type);
        }
        return this.http.get<AssetDto[]>(`${this.apiUrl}/Asset/search`, { params });
    }

    syncAsset(asset: AssetDto): Observable<AssetDto> {
        return this.http.post<AssetDto>(`${this.apiUrl}/Asset/sync`, asset);
    }

    getFiatCurrencies(): Observable<AssetDto[]> {
        return this.http.get<AssetDto[]>(`${this.apiUrl}/Asset/fiat-currencies`);
    }

    getSpotPrice(assetId: string, fiatCurrency: string, date?: Date): Observable<number> {
        let params = new HttpParams().set('fiatCurrency', fiatCurrency);
        if (date) {
            params = params.set('date', date.toISOString());
        }
        return this.http.get<number>(`${this.apiUrl}/Asset/${assetId}/price`, { params });
    }

    getTaxCurrency(): Observable<AssetDto> {
        return this.http.get<AssetDto>(`${this.apiUrl}/Asset/tax-currency`);
    }

    /**
     * Coerces decimal-string fields from the backend into actual JS numbers.
     * The .NET backend serializes `decimal` as JSON strings for precision,
     * but JS arithmetic operators and Angular pipes require real numbers.
     */
    private normalizeProcessedTransaction(pt: ProcessedTransaction, typesMap: Map<string, TransactionType>): ProcessedTransaction {
        return {
            ...pt,
            profitLoss: pt.profitLoss != null ? Number(pt.profitLoss) : undefined,
            totalLossAmount: pt.totalLossAmount != null ? Number(pt.totalLossAmount) : undefined,
            transaction: this.normalizeTransaction(pt.transaction, typesMap)
        };
    }

    private normalizeTransaction(t: Transaction, typesMap: Map<string, TransactionType>): Transaction {
        const typeValue = t.type as unknown as string;
        
        const mappedType: TransactionType = typesMap.get(typeValue) || {
            value: typeValue,
            name: typeValue,
            requiresFromAsset: false,
            requiresToAsset: false
        };

        return {
            ...t,
            type: mappedType,
            amountSpent: Number(t.amountSpent),
            amountReceived: Number(t.amountReceived),
            spotPriceUSD: t.spotPriceUSD != null ? Number(t.spotPriceUSD) : undefined,
            spotPriceEUR: t.spotPriceEUR != null ? Number(t.spotPriceEUR) : undefined,
            fee: Number(t.fee),
            feePriceUSD: t.feePriceUSD != null ? Number(t.feePriceUSD) : undefined,
            feePriceEUR: t.feePriceEUR != null ? Number(t.feePriceEUR) : undefined,
            usdEurExchangeRate: t.usdEurExchangeRate != null ? Number(t.usdEurExchangeRate) : undefined,
        };
    }
}
