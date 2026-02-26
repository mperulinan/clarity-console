import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule, CurrencyPipe, DecimalPipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Router } from '@angular/router';

import { PortfolioService } from '../../services/portfolio.service';
import { PortfolioMetrics } from '../../models/portfolio-metrics';
import { finalize } from 'rxjs';

interface DashboardRow {
    assetId: string;
    name: string;
    symbol: string;
    image?: string;
    price: number;
    holdingsPrice: number;
    holdingsAmount: number;
    avgBuyPrice: number;
    unrealizedPL: number;
    realizedPL: number;
    totalPL: number;
    yieldPercentage: number;
    allocation: number;
    totalCostBasis: number;
}

@Component({
    selector: 'app-dashboard',
    standalone: true,
    imports: [CommonModule, MatButtonModule, MatIconModule, MatTooltipModule, CurrencyPipe, DecimalPipe],
    templateUrl: './dashboard.component.html',
    styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
    dataSource: DashboardRow[] = [];

    totalValue = 0;
    totalCost = 0;
    totalUnrealizedPL = 0;
    totalRealizedPL = 0;
    totalPL = 0;
    totalPLPercentage = 0;

    isLoading = true;

    constructor(
        private portfolioService: PortfolioService,
        private router: Router,
        private cdr: ChangeDetectorRef
    ) { }

    ngOnInit() {
        this.loadData();
    }

    loadData() {
        this.portfolioService.getPortfolioDashboard()
            .pipe(finalize(() => {
                this.isLoading = false;
                this.cdr.detectChanges();
            }))
            .subscribe({
                next: (metrics: PortfolioMetrics) => {
                    if (metrics && metrics.holdings) {
                        this.dataSource = metrics.holdings
                            .map(h => ({
                                assetId: h.id,
                                name: h.name == "" ? h.id : h.name,
                                symbol: h.symbol == "" ? h.id.toUpperCase() : h.symbol.toUpperCase(),
                                image: h.imageUrl,
                                price: h.currentPrice,
                                holdingsPrice: h.currentValue,
                                holdingsAmount: h.quantity,
                                avgBuyPrice: h.avgCost,
                                unrealizedPL: h.openPL,
                                realizedPL: h.realizedPL,
                                totalPL: h.totalPL,
                                yieldPercentage: h.openReturn,
                                allocation: h.allocationPercentage,
                                totalCostBasis: h.totalCostBasis,
                            }))
                            .sort((a, b) => b.holdingsPrice - a.holdingsPrice);

                        this.totalValue = metrics.totalPortfolioValueUsd;
                        this.totalCost = metrics.totalCostBasisUsd;
                        this.totalUnrealizedPL = metrics.totalUnrealizedProfitLossUsd;
                        this.totalRealizedPL = metrics.totalRealizedProfitLossUsd;
                        this.totalPL = metrics.totalProfitLossUsd;
                        this.totalPLPercentage = metrics.totalProfitLossPercentage;
                    } else {
                        this.dataSource = [];
                    }
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
