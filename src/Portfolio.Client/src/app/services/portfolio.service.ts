import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { PortfolioMetrics } from '../models/portfolio-metrics';
import { PortfolioReport } from '../models/portfolio-report';
import { NewTransactionRequest } from '../models/new-transaction-request';
import { environment } from '../../environments/environment';
import { TransactionType, ProcessedTransaction, Transaction } from '../models/transaction';

export interface AssetDto {
    id: string; // The GUID from the DB, or empty if unsynced
    symbol: string;
    name: string;
    externalId?: string;
    imageUrl?: string;
    type: string;
    transactionCount: number;
    marketCapRank?: number;
}

@Injectable({
    providedIn: 'root'
})
export class PortfolioService {
    private apiUrl = environment.apiUrl;

    constructor(private http: HttpClient) { }

    getPortfolioReport(): Observable<PortfolioReport> {
        return this.http.get<PortfolioReport>(`${this.apiUrl}/Portfolio/tax-report`).pipe(
            map(report => ({
                ...report,
                transactions: report.transactions.map(pt => this.normalizeProcessedTransaction(pt))
            }))
        );
    }

    getPortfolioDashboard(): Observable<PortfolioMetrics> {
        return this.http.get<PortfolioMetrics>(`${this.apiUrl}/Portfolio/dashboard`);
    }

    addTransaction(request: NewTransactionRequest): Observable<void> {
        return this.http.post<void>(`${this.apiUrl}/Transaction`, request);
    }

    getTransactionTypes(): Observable<TransactionType[]> {
        return this.http.get<TransactionType[]>(`${this.apiUrl}/Transaction/types`);
    }

    searchAssets(query: string, type?: string): Observable<AssetDto[]> {
        let params = `?query=${encodeURIComponent(query)}`;
        if (type) {
            params += `&type=${encodeURIComponent(type)}`;
        }
        return this.http.get<AssetDto[]>(`${this.apiUrl}/Asset/search${params}`);
    }

    syncAsset(asset: AssetDto): Observable<AssetDto> {
        return this.http.post<AssetDto>(`${this.apiUrl}/Asset/sync`, asset);
    }

    getFiatCurrencies(): Observable<AssetDto[]> {
        return this.http.get<AssetDto[]>(`${this.apiUrl}/Asset/fiat-currencies`);
    }

    /**
     * Coerces decimal-string fields from the backend into actual JS numbers.
     * The .NET backend serializes `decimal` as JSON strings for precision,
     * but JS arithmetic operators and Angular pipes require real numbers.
     */
    private normalizeProcessedTransaction(pt: ProcessedTransaction): ProcessedTransaction {
        return {
            ...pt,
            profitLoss: pt.profitLoss != null ? Number(pt.profitLoss) : undefined,
            totalLossAmount: pt.totalLossAmount != null ? Number(pt.totalLossAmount) : undefined,
            transaction: this.normalizeTransaction(pt.transaction)
        };
    }

    private normalizeTransaction(t: Transaction): Transaction {
        return {
            ...t,
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
