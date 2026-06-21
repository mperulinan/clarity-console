import {
    Component, OnInit, signal, computed,
    ChangeDetectionStrategy, inject, DestroyRef
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DOCUMENT } from '@angular/common';
import { CommonModule, CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatButtonModule } from '@angular/material/button';
import { Router } from '@angular/router';
import { finalize, switchMap, timer, take, takeWhile } from 'rxjs';

import { PortfolioService } from '../../services/portfolio.service';
import { ProcessedTransaction } from '../../models/transaction';
import { DEFAULT_FIAT_CURRENCY } from '../../shared/constants/currency.constants';
import { MatDialog } from '@angular/material/dialog';
import { WashSaleDetailsDialogComponent } from '../../shared/components/wash-sale-details-dialog/wash-sale-details-dialog';

import { SkeletonComponent } from '../../shared/components/skeleton/skeleton.component';
import { BadgeComponent } from '../../shared/components/badge/badge.component';
import { ButtonComponent } from '../../shared/components/button/button.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { getTransactionIcons } from '../../shared/constants/transaction-icons.constants';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';

type SortField = 'date' | 'type' | 'from' | 'to' | 'spotPrice' | 'fee' | 'profitLoss';
type SortDir = 'asc' | 'desc';

@Component({
    selector: 'app-transactions',
    standalone: true,
    imports: [
        CommonModule, MatIconModule, MatTooltipModule, MatButtonModule, 
        CurrencyPipe, DatePipe, DecimalPipe,
        SkeletonComponent, BadgeComponent, ButtonComponent, EmptyStateComponent
    ],
    templateUrl: './transactions.component.html',
    styleUrl: './transactions.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class TransactionsComponent implements OnInit {
    baseCurrency = signal<string>(DEFAULT_FIAT_CURRENCY);

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
                    const priceA = this.baseCurrency() === 'EUR' ? a.transaction.spotPriceEUR : a.transaction.spotPriceUSD;
                    const priceB = this.baseCurrency() === 'EUR' ? b.transaction.spotPriceEUR : b.transaction.spotPriceUSD;
                    cmp = (priceA ?? 0) - (priceB ?? 0);
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

    // Helper for icons
    getTransactionIcons = getTransactionIcons;

    private readonly destroyRef = inject(DestroyRef);
    private readonly dialog = inject(MatDialog);

    private readonly portfolioService = inject(PortfolioService);
    private readonly router = inject(Router);
    private readonly document = inject(DOCUMENT);

    ngOnInit() {
        this.loadTransactions();
    }

    private loadTransactions() {
        this.isLoading.set(true);
        this.portfolioService.getPortfolioReport()
            .pipe(
                finalize(() => this.isLoading.set(false)),
                takeUntilDestroyed(this.destroyRef)
            )
            .subscribe({
                next: report => {
                    this.baseCurrency.set(report.reportingCurrency);
                    this.allTransactions.set(report.transactions);
                    this.pollMissingSpotPrices();
                },
                error: err => console.error('Failed to load transactions', err)
            });
    }

    private pollMissingSpotPrices() {
        const missingIds = this.allTransactions()
            .map(t => t.transaction)
            .filter(t => t.spotPriceEUR == null || t.spotPriceUSD == null)
            .map(t => t.id);

        if (missingIds.length === 0) return;

        missingIds.forEach(id => {
            // Poll every 1.5 seconds, but stop as soon as we get the exchange rate (or max 3 tries)
            timer(1500, 1500).pipe(
                switchMap(() => this.portfolioService.getTransaction(id)),
                takeWhile(updatedTx => updatedTx.spotPriceEUR == null || updatedTx.spotPriceUSD == null, true),
                take(3),
                takeUntilDestroyed(this.destroyRef)
            ).subscribe({
                next: updatedTx => {
                    if (updatedTx.spotPriceEUR != null || updatedTx.spotPriceUSD != null) {
                        this.allTransactions.update(all => all.map(pt => 
                            pt.transaction.id === updatedTx.id 
                                ? { ...pt, transaction: updatedTx } 
                                : pt
                        ));
                    }
                }
            });
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

    getAriaSort(field: SortField): 'ascending' | 'descending' | 'none' {
        if (this.sortField() !== field) return 'none';
        return this.sortDir() === 'asc' ? 'ascending' : 'descending';
    }

    openNewTransaction() {
        this.router.navigate(['/new-transaction']);
    }

    editTransaction(id: number) {
        this.router.navigate(['/edit-transaction', id]);
    }

    deleteTransaction(id: number) {
        const dialogRef = this.dialog.open(ConfirmDialogComponent, {
            data: {
                title: 'Delete Transaction',
                message: 'Are you sure you want to delete this transaction? This action cannot be undone.',
                confirmText: 'Delete'
            },
            panelClass: 'dark-dialog-panel'
        });

        dialogRef.afterClosed().subscribe(confirmed => {
            if (confirmed) {
                this.portfolioService.deleteTransaction(id).subscribe({
                    next: () => {
                        this.loadTransactions();
                    },
                    error: err => console.error('Failed to delete transaction', err)
                });
            }
        });
    }

    getSpotPrice(row: ProcessedTransaction): number | undefined {
        return this.baseCurrency() === 'EUR' ? row.transaction.spotPriceEUR : row.transaction.spotPriceUSD;
    }

    viewWashSaleDetails(row: ProcessedTransaction) {
        if (!row.disallowedByTransactionId) return;

        const targetElementId = `tx-${row.disallowedByTransactionId}`;
        const element = this.document.getElementById(targetElementId);

        if (element) {
            element.scrollIntoView({ behavior: 'smooth', block: 'center' });
            element.classList.add('highlight-glow');
            timer(2500).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
                element.classList.remove('highlight-glow');
            });
            return;
        }

        const repurchaseTx = this.allTransactions().find(t => t.transaction.id === row.disallowedByTransactionId);
        
        if (repurchaseTx) {
            this.dialog.open(WashSaleDetailsDialogComponent, {
                width: '600px',
                data: {
                    lossTx: row,
                    repurchaseTx: repurchaseTx,
                    currency: this.baseCurrency()
                },
                panelClass: 'dark-dialog-panel'
            });
        }
    }
}
