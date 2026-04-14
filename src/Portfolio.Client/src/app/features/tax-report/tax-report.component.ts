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

interface YearSummary {
    year: number;
    netPL: number;
    disallowedLosses: number;
    eventCount: number;
    errorCount: number;
}

@Component({
    selector: 'app-tax-report',
    standalone: true,
    imports: [CommonModule, MatIconModule, MatTooltipModule, CurrencyPipe, DatePipe, DecimalPipe],
    templateUrl: './tax-report.component.html',
    styleUrl: './tax-report.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class TaxReportComponent implements OnInit {
    readonly reportCurrency = 'EUR';

    private allTransactions = signal<ProcessedTransaction[]>([]);
    isLoading = signal(true);
    selectedYear = signal<number | null>(null);

    yearSummaries = computed<YearSummary[]>(() => {
        const txs = this.allTransactions();
        const yearMap = new Map<number, ProcessedTransaction[]>();

        for (const pt of txs) {
            const year = new Date(pt.transaction.date).getFullYear();
            if (!yearMap.has(year)) yearMap.set(year, []);
            yearMap.get(year)!.push(pt);
        }

        const summaries: YearSummary[] = [];
        for (const [year, pts] of yearMap) {
            let netPL = 0;
            let disallowedLosses = 0;
            let errorCount = 0;

            for (const pt of pts) {
                if (pt.profitLoss != null && !pt.isLossDisallowed) {
                    netPL += pt.profitLoss;
                }
                if (pt.isLossDisallowed && pt.profitLoss != null) {
                    disallowedLosses += Math.abs(pt.profitLoss);
                }
                if (pt.error) errorCount++;
            }

            summaries.push({ year, netPL, disallowedLosses, eventCount: pts.length, errorCount });
        }

        return summaries.sort((a, b) => b.year - a.year);
    });

    filteredTransactions = computed(() => {
        const year = this.selectedYear();
        const txs = this.allTransactions();
        if (year === null) return txs;
        return txs.filter(pt => new Date(pt.transaction.date).getFullYear() === year);
    });

    filteredCount = computed(() => this.filteredTransactions().length);

    private readonly destroyRef = inject(DestroyRef);

    constructor(private portfolioService: PortfolioService) { }

    ngOnInit() {
        this.portfolioService.getPortfolioReport()
            .pipe(
                finalize(() => this.isLoading.set(false)),
                takeUntilDestroyed(this.destroyRef)
            )
            .subscribe({
                next: report => {
                    this.allTransactions.set(report.transactions);
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
}
