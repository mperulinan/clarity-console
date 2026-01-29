import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { Router } from '@angular/router';

import { PortfolioService } from '../../services/portfolio.service';
import { PortfolioMetrics } from '../../models/portfolio-metrics';
import { finalize } from 'rxjs';

interface DashboardRow {
    assetId: string;
    name: string;
    symbol: string;
    image: string;
    price: number;
    holdingsPrice: number;
    holdingsAmount: number;
    avgBuyPrice: number;
    yield: number; // P/L
    yieldPercentage: number;
    allocation: number;
}

@Component({
    selector: 'app-dashboard',
    standalone: true,
    imports: [CommonModule, MatTableModule, MatButtonModule, MatIconModule, MatCardModule],
    templateUrl: './dashboard.component.html',
    styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
    displayedColumns: string[] = ['assetId', 'price', 'holdings', 'avgBuyPrice', 'yield', 'percentage'];
    dataSource: DashboardRow[] = [];

    totalValue = 0;
    totalCost = 0;
    totalPL = 0;
    totalPLPercentage = 0;

    isLoading = true;

    constructor(
        private portfolioService: PortfolioService,
        private router: Router,
        private cdr: ChangeDetectorRef
    ) { }

    async ngOnInit() {
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
                    console.log('Metrics received:', metrics);

                    if (metrics && metrics.holdings) {
                        this.dataSource = metrics.holdings.map(h => ({
                            assetId: h.assetId,
                            name: h.assetId ? h.assetId.toUpperCase() : 'UNKNOWN',
                            symbol: h.assetId ? h.assetId.toUpperCase() : '???',
                            image: '',
                            price: h.currentPriceUsd,
                            holdingsPrice: h.currentValueUsd,
                            holdingsAmount: h.quantity,
                            avgBuyPrice: h.avgCostUsd,
                            yield: h.totalProfitLossUsd,
                            yieldPercentage: h.yieldPercentage,
                            allocation: h.allocationPercentage
                        })).sort((a, b) => b.holdingsPrice - a.holdingsPrice);

                        this.totalValue = metrics.totalPortfolioValueUsd;
                        this.totalCost = metrics.totalCostBasisUsd;
                        this.totalPL = metrics.totalProfitLossUsd;
                        this.totalPLPercentage = metrics.totalProfitLossPercentage;
                    } else {
                        console.warn('Metrics or holdings missing/empty', metrics);
                        this.dataSource = [];
                    }
                },
                error: (err) => {
                    console.error('Error loading dashboard data:', err);
                }
            });
    }

    openNewTransaction() {
        this.router.navigate(['/new-transaction']);
    }
}
