import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PortfolioMetrics } from '../models/portfolio-metrics';
import { PortfolioReport } from '../models/portfolio-report';
import { NewTransactionRequest } from '../models/new-transaction-request';

@Injectable({
    providedIn: 'root'
})
export class PortfolioService {
    private apiUrl = 'http://localhost:5284/api';

    constructor(private http: HttpClient) { }

    getPortfolioReport(): Observable<PortfolioReport> {
        return this.http.get<PortfolioReport>(`${this.apiUrl}/Portfolio/tax-report`);
    }

    getPortfolioDashboard(): Observable<PortfolioMetrics> {
        return this.http.get<PortfolioMetrics>(`${this.apiUrl}/Portfolio/dashboard`);
    }

    addTransaction(request: NewTransactionRequest): Observable<void> {
        return this.http.post<void>(`${this.apiUrl}/Transaction`, request);
    }
}
