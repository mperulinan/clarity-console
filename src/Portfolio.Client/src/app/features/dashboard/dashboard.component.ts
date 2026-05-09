import { Component, OnInit, signal, computed, ChangeDetectionStrategy, inject, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule, CurrencyPipe, DecimalPipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Router } from '@angular/router';
import { SkeletonComponent } from '../../shared/components/skeleton/skeleton.component';
import { BadgeComponent } from '../../shared/components/badge/badge.component';
import { ButtonComponent } from '../../shared/components/button/button.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';

import { PortfolioService } from '../../services/portfolio.service';
import { PortfolioMetrics } from '../../models/portfolio-metrics';
import { finalize } from 'rxjs';
import { mapToDashboardRows } from './dashboard.mapper';
import { DashboardRow } from './dashboard-row';
import { DEFAULT_FIAT_CURRENCY } from '../../shared/constants/currency.constants';

@Component({
    selector: 'app-dashboard',
    standalone: true,
    imports: [CommonModule, MatButtonModule, MatIconModule, MatTooltipModule, CurrencyPipe, DecimalPipe, SkeletonComponent, BadgeComponent, ButtonComponent, EmptyStateComponent],
    templateUrl: './dashboard.component.html',
    styleUrl: './dashboard.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class DashboardComponent implements OnInit {
    /** Base currency for all monetary displays — sourced from the global constant. */
    readonly baseCurrency = DEFAULT_FIAT_CURRENCY;

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
    netDeposits = computed(() => this.metrics()?.netDepositsUsd ?? 0);
    totalUnrealizedPL = computed(() => this.metrics()?.totalUnrealizedProfitLossUsd ?? 0);
    totalRealizedPL = computed(() => this.metrics()?.totalRealizedProfitLossUsd ?? 0);
    totalPL = computed(() => this.metrics()?.totalProfitLossUsd ?? 0);
    unrealizedPLPercentage = computed(() => this.metrics()?.unrealizedProfitLossPercentage ?? 0);
    totalReturn = computed(() => this.metrics()?.totalReturn ?? 0);

    // ── Boolean state signals (keep template free of arithmetic) ─────────
    isPlPositive = computed(() => this.totalPL() >= 0);
    isUnrealizedPositive = computed(() => this.totalUnrealizedPL() >= 0);
    isRealizedPositive = computed(() => this.totalRealizedPL() >= 0);
    isReturnPositive = computed(() => this.totalReturn() >= 0);

    // ── Sign prefix helpers ───────────────────────────────────────────────
    plSign = computed(() => this.isPlPositive() ? '+' : '');
    unrealizedSign = computed(() => this.isUnrealizedPositive() ? '+' : '');
    realizedSign = computed(() => this.isRealizedPositive() ? '+' : '');
    returnSign = computed(() => this.isReturnPositive() ? '+' : '');

    private readonly destroyRef = inject(DestroyRef);

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
            .pipe(
                finalize(() => this.isLoading.set(false)),
                takeUntilDestroyed(this.destroyRef)
            )
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
        return (symbol || '??').slice(0, 2).toUpperCase();
    }

    openNewTransaction() {
        this.router.navigate(['/new-transaction']);
    }
}
