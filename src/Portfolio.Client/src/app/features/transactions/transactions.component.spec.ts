import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TransactionsComponent } from './transactions.component';
import { PortfolioService } from '../../services/portfolio.service';
import { Router } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of, throwError } from 'rxjs';
import { ProcessedTransaction, TransactionType } from '../../models/transaction';
import { PortfolioReport } from '../../models/portfolio-report';

describe('TransactionsComponent', () => {
  let component: TransactionsComponent;
  let fixture: ComponentFixture<TransactionsComponent>;
  let portfolioServiceSpy: any;
  let routerSpy: any;
  let dialogSpy: any;

  const mockTxType: TransactionType = { value: 'BUY', name: 'Buy', requiresFromAsset: false, requiresToAsset: true, requiresSpotPrice: true };

  const makeTx = (id: number, date: string, profitLoss?: number): ProcessedTransaction => ({
    transaction: {
      id, date, type: mockTxType,
      amountSpent: 0, amountReceived: 1, fee: 0
    },
    profitLoss,
    disallowsPreviousLosses: [],
    isLossDisallowed: false
  });

  const mockReport: PortfolioReport = {
    reportingCurrency: 'USD',
    transactions: [
      makeTx(1, '2024-03-01T00:00:00Z', 100),
      makeTx(2, '2024-01-01T00:00:00Z', -50),
    ],
    holdings: [],
    yearSummaries: []
  };

  beforeEach(async () => {
    // Reset history state so loadTransactions doesn't encounter null
    history.replaceState({}, '');

    portfolioServiceSpy = {
      getPortfolioReport:        () => of(mockReport),
      getProcessedTransactions:  () => of(mockReport.transactions),
      deleteTransaction:         vi.fn().mockReturnValue(of(null)),
    };
    routerSpy = { navigate: vi.fn() };
    dialogSpy = { open: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [TransactionsComponent, NoopAnimationsModule],
      providers: [
        { provide: PortfolioService, useValue: portfolioServiceSpy },
        { provide: Router,           useValue: routerSpy },
        { provide: MatDialog,        useValue: dialogSpy }
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(TransactionsComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create and render template rows', () => {
    expect(component).toBeTruthy();
    fixture.detectChanges();
    const rows = fixture.nativeElement.querySelectorAll('.txn-row');
    expect(rows.length).toBe(2);
  });

  it('should load transactions and set loading to false', async () => {
    expect(component.isLoading()).toBe(false);
    expect(component.totalCount()).toBe(2);
    expect(component.baseCurrency).toBe('USD');
  });

  it('loadTransactions() should handle service error', () => {
    portfolioServiceSpy.getProcessedTransactions = vi.fn().mockReturnValue(throwError(() => new Error('Load failed')));
    (component as any).loadTransactions();
    expect(component.isLoading()).toBe(false);
  });

  it('should sort by date descending by default', async () => {
    const txs = component.transactions();
    expect(txs[0].transaction.id).toBe(1); // 2024-03-01 is newer
    expect(txs[1].transaction.id).toBe(2);
  });

  it('toggleSort() should toggle direction when clicking the same field', async () => {
    expect(component.sortDir()).toBe('desc');
    component.toggleSort('date');
    expect(component.sortDir()).toBe('asc');
    component.toggleSort('date');
    expect(component.sortDir()).toBe('desc');
  });

  it('toggleSort() should switch field and set ascending for non-date fields', async () => {
    component.toggleSort('profitLoss');
    expect(component.sortField()).toBe('profitLoss');
    expect(component.sortDir()).toBe('asc');
  });

  it('getSortIcon() should return correct icon based on state', () => {
    expect(component.getSortIcon('date')).toBe('arrow_downward'); // default: date desc
    expect(component.getSortIcon('type')).toBe('unfold_more');    // non-active field
  });

  it('getAriaSort() should return correct value', () => {
    expect(component.getAriaSort('date')).toBe('descending');
    expect(component.getAriaSort('type')).toBe('none');
  });

  it('openNewTransaction() should navigate to /new-transaction', () => {
    component.openNewTransaction();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/new-transaction']);
  });

  it('editTransaction() should navigate to /edit-transaction/:id', () => {
    component.editTransaction(42);
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/edit-transaction', 42]);
  });

  it('getSpotPrice() should return EUR price when baseCurrency is EUR', async () => {
    (component as any).baseCurrency = 'EUR';
    const tx = makeTx(99, '2024-01-01T00:00:00Z');
    tx.transaction.spotPriceEUR = 40000;
    tx.transaction.spotPriceUSD = 45000;
    expect(component.getSpotPrice(tx)).toBe(40000);
  });

  it('getSpotPrice() should return USD price when baseCurrency is USD', async () => {
    (component as any).baseCurrency = 'USD';
    const tx = makeTx(99, '2024-01-01T00:00:00Z');
    tx.transaction.spotPriceEUR = 40000;
    tx.transaction.spotPriceUSD = 45000;
    expect(component.getSpotPrice(tx)).toBe(45000);
  });

  it('spotPriceIsRelevant() should return true when type requiresSpotPrice', () => {
    const tx = makeTx(1, '2024-01-01T00:00:00Z');
    expect(component.spotPriceIsRelevant(tx)).toBe(true);
  });

  it('spotPriceIsRelevant() should return false when type does not requiresSpotPrice', () => {
    const tx = makeTx(1, '2024-01-01T00:00:00Z');
    tx.transaction.type = { ...mockTxType, requiresSpotPrice: false };
    expect(component.spotPriceIsRelevant(tx)).toBe(false);
  });

  // ── deleteTransaction ─────────────────────────────────────────────────

  it('deleteTransaction() should open confirm dialog', () => {
    dialogSpy.open.mockReturnValue({ afterClosed: () => of(false) });
    component.deleteTransaction(1);
    expect(dialogSpy.open).toHaveBeenCalled();
  });

  it('deleteTransaction() should call service and reload when confirmed', () => {
    dialogSpy.open.mockReturnValue({ afterClosed: () => of(true) });
    component.deleteTransaction(1);
    expect(portfolioServiceSpy.deleteTransaction).toHaveBeenCalledWith(1);
  });

  it('deleteTransaction() should not call service when cancelled', () => {
    dialogSpy.open.mockReturnValue({ afterClosed: () => of(false) });
    component.deleteTransaction(1);
    expect(portfolioServiceSpy.deleteTransaction).not.toHaveBeenCalled();
  });

  it('deleteTransaction() should handle service error', () => {
    dialogSpy.open.mockReturnValue({ afterClosed: () => of(true) });
    portfolioServiceSpy.deleteTransaction.mockReturnValue(throwError(() => new Error('Delete failed')));
    component.deleteTransaction(1);
    expect(portfolioServiceSpy.deleteTransaction).toHaveBeenCalledWith(1);
  });

  // ── viewWashSaleDetails ───────────────────────────────────────────────

  it('viewWashSaleDetails() should do nothing if no disallowedByTransactionId', () => {
    const tx = makeTx(1, '2024-01-01T00:00:00Z');
    // No disallowedByTransactionId set
    component.viewWashSaleDetails(tx);
    expect(dialogSpy.open).not.toHaveBeenCalled();
  });

  it('viewWashSaleDetails() should open dialog when repurchase tx is in list but element not in DOM', () => {
    // Use id=99 so no DOM element exists for it
    const repurchaseTx = makeTx(99, '2024-01-15T00:00:00Z');
    // Inject repurchaseTx into allTransactions via the service
    portfolioServiceSpy.getProcessedTransactions = () => of([...mockReport.transactions, repurchaseTx]);

    dialogSpy.open.mockReturnValue({ afterClosed: () => of(false) });

    // Reload so allTransactions includes repurchaseTx
    (component as any).loadTransactions(true);

    const lossTx = makeTx(1, '2024-01-01T00:00:00Z');
    lossTx.disallowedByTransactionId = 99;

    component.viewWashSaleDetails(lossTx);

    expect(dialogSpy.open).toHaveBeenCalled();
  });

  it('viewWashSaleDetails() should scroll and not open dialog when element is found in DOM', () => {
    const scrollSpy = vi.fn();
    window.HTMLElement.prototype.scrollIntoView = scrollSpy;

    const lossTx = makeTx(1, '2024-01-01T00:00:00Z');
    lossTx.disallowedByTransactionId = 2;

    component.viewWashSaleDetails(lossTx);

    expect(scrollSpy).toHaveBeenCalled();
    expect(dialogSpy.open).not.toHaveBeenCalled();
  });

  // ── Sorting coverage for remaining fields ─────────────────────────────

  it('toggleSort() should sort by type, from, to, spotPrice, and fee', () => {
    for (const field of ['type', 'from', 'to', 'spotPrice', 'fee'] as const) {
      component.toggleSort(field);
      expect(component.sortField()).toBe(field);
    }
  });

  it('getSortIcon() should return arrow_upward when sorting ascending', () => {
    component.toggleSort('profitLoss'); // sets asc
    expect(component.getSortIcon('profitLoss')).toBe('arrow_upward');
  });

  it('getAriaSort() should return ascending when sorting asc', () => {
    component.toggleSort('profitLoss');
    expect(component.getAriaSort('profitLoss')).toBe('ascending');
  });

  it('openImport() should navigate to /import', () => {
    component.openImport();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/import']);
  });

  it('renders an Import CSV button in the page header', () => {
    fixture.detectChanges();
    const buttons: NodeListOf<HTMLElement> = fixture.nativeElement.querySelectorAll('app-button');
    const hasImportBtn = Array.from(buttons).some(b => b.textContent?.includes('Import CSV'));
    expect(hasImportBtn).toBe(true);
  });
});
