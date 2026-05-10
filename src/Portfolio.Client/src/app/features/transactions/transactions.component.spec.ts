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

  const mockTxType: TransactionType = { value: 'BUY', name: 'Buy', requiresFromAsset: false, requiresToAsset: true };

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
    portfolioServiceSpy = {
      getPortfolioReport: () => of(mockReport)
    };
    routerSpy = { navigate: vi.fn() };
    dialogSpy = { open: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [TransactionsComponent, NoopAnimationsModule],
      providers: [
        { provide: PortfolioService, useValue: portfolioServiceSpy },
        { provide: Router, useValue: routerSpy },
        { provide: MatDialog, useValue: dialogSpy }
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(TransactionsComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load transactions and set loading to false', async () => {
    expect(component.isLoading()).toBe(false);
    expect(component.totalCount()).toBe(2);
    expect(component.baseCurrency()).toBe('USD');
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
    expect(component.getSortIcon('type')).toBe('unfold_more'); // non-active field
  });

  it('getAriaSort() should return correct value', () => {
    expect(component.getAriaSort('date')).toBe('descending');
    expect(component.getAriaSort('type')).toBe('none');
  });

  it('openNewTransaction() should navigate to /new-transaction', () => {
    component.openNewTransaction();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/new-transaction']);
  });

  it('getSpotPrice() should return EUR price when baseCurrency is EUR', async () => {
    component.baseCurrency.set('EUR');
    const tx = makeTx(99, '2024-01-01T00:00:00Z');
    tx.transaction.spotPriceEUR = 40000;
    tx.transaction.spotPriceUSD = 45000;
    expect(component.getSpotPrice(tx)).toBe(40000);
  });

  it('getSpotPrice() should return USD price when baseCurrency is USD', async () => {
    component.baseCurrency.set('USD');
    const tx = makeTx(99, '2024-01-01T00:00:00Z');
    tx.transaction.spotPriceEUR = 40000;
    tx.transaction.spotPriceUSD = 45000;
    expect(component.getSpotPrice(tx)).toBe(45000);
  });
});
