import {
    Component, OnInit, signal, computed,
    ChangeDetectionStrategy, inject, DestroyRef
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule, CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { finalize } from 'rxjs';

import { PortfolioService } from '../../services/portfolio.service';
import { ProcessedTransaction } from '../../models/transaction';
import { YearSummary } from '../../models/portfolio-report';
import { DEFAULT_FIAT_CURRENCY } from '../../shared/constants/currency.constants';
import { MatDialog } from '@angular/material/dialog';
import { WashSaleDetailsDialogComponent } from '../../shared/components/wash-sale-details-dialog/wash-sale-details-dialog';
import { SkeletonComponent } from '../../shared/components/skeleton/skeleton.component';
import { BadgeComponent } from '../../shared/components/badge/badge.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';

/**
 * Icon mapping per transaction type — consistent with new-transaction's UI_CONFIG.
 */
const EVENT_ICONS: Record<string, { fromIcon: string; toIcon: string }> = {
    SWAP: { fromIcon: 'sell', toIcon: 'shopping_cart' },
    WITHDRAWAL: { fromIcon: 'north_east', toIcon: '' },
    DEPOSIT: { fromIcon: '', toIcon: 'south_east' },
    REWARD: { fromIcon: '', toIcon: 'workspace_premium' },
};

@Component({
    selector: 'app-tax-report',
    standalone: true,
    imports: [
        CommonModule, MatIconModule, MatTooltipModule, CurrencyPipe, DatePipe, DecimalPipe,
        SkeletonComponent, BadgeComponent, EmptyStateComponent
    ],
    templateUrl: './tax-report.component.html',
    styleUrl: './tax-report.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class TaxReportComponent implements OnInit {
    reportCurrency = signal<string>(DEFAULT_FIAT_CURRENCY); // Default fallback, overwritten by backend

    private allTransactions = signal<ProcessedTransaction[]>([]);
    isLoading = signal(true);
    selectedYear = signal<number | null>(null);

    /** Year summaries provided by the backend — no frontend business logic. */
    yearSummaries = signal<YearSummary[]>([]);

    /** KPIs for the currently selected year (or all years if none selected). */
    selectedYearKPIs = computed(() => {
        const year = this.selectedYear();
        const summaries = this.yearSummaries();
        if (year === null) {
            // Aggregate all years
            let totalGains = 0, totalLosses = 0, disallowed = 0;
            for (const ys of summaries) {
                totalGains += ys.totalGains;
                totalLosses += ys.totalLosses;
                disallowed += ys.disallowedLosses;
            }
            return { totalGains, totalLosses, netPL: totalGains + totalLosses, disallowed };
        }
        const ys = summaries.find(s => s.year === year);
        if (!ys) return { totalGains: 0, totalLosses: 0, netPL: 0, disallowed: 0 };
        return { totalGains: ys.totalGains, totalLosses: ys.totalLosses, netPL: ys.netPL, disallowed: ys.disallowedLosses };
    });

    filteredTransactions = computed(() => {
        const year = this.selectedYear();
        const txs = this.allTransactions();
        if (year === null) return txs;
        return txs.filter(pt => new Date(pt.transaction.date).getFullYear() === year);
    });

    filteredCount = computed(() => this.filteredTransactions().length);

    private readonly destroyRef = inject(DestroyRef);
    private dialog = inject(MatDialog);

    constructor(private portfolioService: PortfolioService) { }

    ngOnInit() {
        this.portfolioService.getPortfolioReport()
            .pipe(
                finalize(() => this.isLoading.set(false)),
                takeUntilDestroyed(this.destroyRef)
            )
            .subscribe({
                next: report => {
                    this.reportCurrency.set(report.reportingCurrency);
                    this.allTransactions.set(report.transactions);
                    this.yearSummaries.set(report.yearSummaries || []);

                    // Auto-select the most recent year
                    const summaries = this.yearSummaries();
                    if (summaries.length > 0) {
                        this.selectedYear.set(summaries[0].year);
                    }
                },
                error: err => console.error('Failed to load tax report', err)
            });
    }

    selectYear(year: number) {
        this.selectedYear.set(year);
    }

    clearYearFilter() {
        this.selectedYear.set(null);
    }

    getSpotPrice(row: ProcessedTransaction): number | undefined {
        const currency = this.reportCurrency();
        return currency === 'EUR' ? row.transaction.spotPriceEUR : row.transaction.spotPriceUSD;
    }

    getEventIcons(typeValue: string): { fromIcon: string; toIcon: string } {
        return EVENT_ICONS[typeValue?.toUpperCase()] || { fromIcon: 'swap_horiz', toIcon: 'swap_horiz' };
    }

    viewWashSaleDetails(row: ProcessedTransaction) {
        if (!row.disallowedByTransactionId) return;

        // Pattern 1: Try to find the related transaction element on screen to scroll to it
        const targetElementId = `tx-${row.disallowedByTransactionId}`;
        const element = document.getElementById(targetElementId);

        if (element) {
            // It's in the current view. Auto-scroll and glow.
            element.scrollIntoView({ behavior: 'smooth', block: 'center' });
            element.classList.add('highlight-glow');
            setTimeout(() => {
                element.classList.remove('highlight-glow');
            }, 2500);
            return;
        }

        // Pattern 2: It's filtered out. Find the data in memory and launch the Forensic Dialog.
        const repurchaseTx = this.allTransactions().find(t => t.transaction.id === row.disallowedByTransactionId);
        
        if (repurchaseTx) {
            this.dialog.open(WashSaleDetailsDialogComponent, {
                width: '600px',
                data: {
                    lossTx: row,
                    repurchaseTx: repurchaseTx,
                    currency: this.reportCurrency()
                },
                panelClass: 'dark-dialog-panel'
            });
        }
    }
}
