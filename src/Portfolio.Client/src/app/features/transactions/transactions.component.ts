import {
    Component, OnInit, signal, computed,
    ChangeDetectionStrategy, inject, DestroyRef
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule, CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatButtonModule } from '@angular/material/button';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';

import { PortfolioService } from '../../services/portfolio.service';
import { ProcessedTransaction } from '../../models/transaction';
import { DEFAULT_FIAT_CURRENCY } from '../../shared/constants/currency.constants';

type SortField = 'date' | 'type' | 'from' | 'to' | 'spotPrice' | 'fee' | 'profitLoss';
type SortDir = 'asc' | 'desc';

@Component({
    selector: 'app-transactions',
    standalone: true,
    imports: [CommonModule, MatIconModule, MatTooltipModule, MatButtonModule, CurrencyPipe, DatePipe, DecimalPipe],
    templateUrl: './transactions.component.html',
    styleUrl: './transactions.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class TransactionsComponent implements OnInit {
    readonly baseCurrency = DEFAULT_FIAT_CURRENCY;

    private allTransactions = signal<ProcessedTransaction[]>([]);
    isLoading = signal(true);
    sortField = signal<SortField>('date');
    sortDir = signal<SortDir>('desc');

    transactions = computed(() => {
        const txs = [...this.allTransactions()];
        const field = this.sortField();
        const dir = this.sortDir();

        txs.sort((a, b) => {
            let cmp = 0;
            switch (field) {
                case 'date':
                    cmp = new Date(a.transaction.date).getTime() - new Date(b.transaction.date).getTime();
                    break;
                case 'type':
                    cmp = (a.transaction.type.name ?? '').localeCompare(b.transaction.type.name ?? '');
                    break;
                case 'from':
                    cmp = (a.transaction.fromAsset?.symbol ?? '').localeCompare(b.transaction.fromAsset?.symbol ?? '');
                    break;
                case 'to':
                    cmp = (a.transaction.toAsset?.symbol ?? '').localeCompare(b.transaction.toAsset?.symbol ?? '');
                    break;
                case 'spotPrice':
                    cmp = (a.transaction.spotPriceUSD ?? 0) - (b.transaction.spotPriceUSD ?? 0);
                    break;
                case 'fee':
                    cmp = a.transaction.fee - b.transaction.fee;
                    break;
                case 'profitLoss':
                    cmp = (a.profitLoss ?? 0) - (b.profitLoss ?? 0);
                    break;
            }
            return dir === 'asc' ? cmp : -cmp;
        });

        return txs;
    });

    totalCount = computed(() => this.allTransactions().length);

    private readonly destroyRef = inject(DestroyRef);

    constructor(
        private portfolioService: PortfolioService,
        private router: Router
    ) { }

    ngOnInit() {
        this.portfolioService.getPortfolioReport()
            .pipe(
                finalize(() => this.isLoading.set(false)),
                takeUntilDestroyed(this.destroyRef)
            )
            .subscribe({
                next: report => this.allTransactions.set(report.transactions),
                error: err => console.error('Failed to load transactions', err)
            });
    }

    toggleSort(field: SortField) {
        if (this.sortField() === field) {
            this.sortDir.update(d => d === 'asc' ? 'desc' : 'asc');
        } else {
            this.sortField.set(field);
            this.sortDir.set(field === 'date' ? 'desc' : 'asc');
        }
    }

    getSortIcon(field: SortField): string {
        if (this.sortField() !== field) return 'unfold_more';
        return this.sortDir() === 'asc' ? 'arrow_upward' : 'arrow_downward';
    }

    openNewTransaction() {
        this.router.navigate(['/new-transaction']);
    }
}
