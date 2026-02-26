import { Component, OnInit, signal, computed, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule, CurrencyPipe, DecimalPipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Router } from '@angular/router';

import { PortfolioService } from '../../services/portfolio.service';
import { PortfolioMetrics } from '../../models/portfolio-metrics';
import { finalize } from 'rxjs';
import { mapToDashboardRows } from './dashboard.mapper';
import { DashboardRow } from './dashboard-row';

@Component({
    selector: 'app-dashboard',
    standalone: true,
    imports: [CommonModule, MatButtonModule, MatIconModule, MatTooltipModule, CurrencyPipe, DecimalPipe],
    templateUrl: './dashboard.component.html',
    styleUrl: './dashboard.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class DashboardComponent implements OnInit {
    // Signals for state
    private metrics = signal<PortfolioMetrics | null>(null);
    isLoading = signal<boolean>(true);

    // Computed signals for UI
    dataSource = computed<DashboardRow[]>(() => {
        const data = this.metrics();
        return data ? mapToDashboardRows(data) : [];
    });

    totalValue = computed(() => this.metrics()?.totalPortfolioValueUsd ?? 0);
    totalCost = computed(() => this.metrics()?.totalCostBasisUsd ?? 0);
    totalUnrealizedPL = computed(() => this.metrics()?.totalUnrealizedProfitLossUsd ?? 0);
    totalRealizedPL = computed(() => this.metrics()?.totalRealizedProfitLossUsd ?? 0);
    totalPL = computed(() => this.metrics()?.totalProfitLossUsd ?? 0);
    totalPLPercentage = computed(() => this.metrics()?.totalProfitLossPercentage ?? 0);

    constructor(
        private portfolioService: PortfolioService,
        private router: Router
    ) { }

    ngOnInit() {
        this.loadData();
    }

    loadData() {
        this.isLoading.set(true);
        this.portfolioService.getPortfolioDashboard()
            .pipe(finalize(() => this.isLoading.set(false)))
            .subscribe({
                next: (metrics: PortfolioMetrics) => {
                    this.metrics.set(metrics);
                },
                error: (err) => {
                    console.error('Error loading dashboard data:', err);
                }
            });
    }

    getInitials(symbol: string): string {
        return (symbol ?? '??').slice(0, 2).toUpperCase();
    }

    openNewTransaction() {
        this.router.navigate(['/new-transaction']);
    }
}
