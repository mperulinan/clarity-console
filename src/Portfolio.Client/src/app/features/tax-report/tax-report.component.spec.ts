import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TaxReportComponent } from './tax-report.component';
import { PortfolioService } from '../../services/portfolio.service';
import { MatDialog } from '@angular/material/dialog';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { ProcessedTransaction, TransactionType } from '../../models/transaction';
import { PortfolioReport, YearSummary } from '../../models/portfolio-report';

describe('TaxReportComponent', () => {
  let component: TaxReportComponent;
  let fixture: ComponentFixture<TaxReportComponent>;
  let portfolioServiceSpy: any;
  let dialogSpy: any;

  const mockTxType: TransactionType = { value: 'BUY', name: 'Buy', requiresFromAsset: false, requiresToAsset: true };

  const makeTx = (id: number, date: string, profitLoss?: number): ProcessedTransaction => ({
    transaction: { id, date, type: mockTxType, amountSpent: 0, amountReceived: 1, fee: 0 },
    profitLoss,
    disallowsPreviousLosses: [],
    isLossDisallowed: false
  });

  const mockYearSummaries: YearSummary[] = [
    { year: 2024, totalGains: 5000, totalLosses: -2000, netPL: 3000, disallowedLosses: 500, eventCount: 1, errorCount: 0 },
    { year: 2023, totalGains: 3000, totalLosses: -1000, netPL: 2000, disallowedLosses: 0, eventCount: 1, errorCount: 0 },
  ];

  const mockReport: PortfolioReport = {
    reportingCurrency: 'EUR',
    transactions: [
      makeTx(1, '2024-05-01T00:00:00Z', 1000),
      makeTx(2, '2023-06-15T00:00:00Z', -500),
    ],
    holdings: [],
    yearSummaries: mockYearSummaries
  };

  beforeEach(async () => {
    portfolioServiceSpy = {
      getPortfolioReport: () => of(mockReport)
    };
    dialogSpy = { open: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [TaxReportComponent, NoopAnimationsModule],
      providers: [
        { provide: PortfolioService, useValue: portfolioServiceSpy },
        { provide: MatDialog, useValue: dialogSpy }
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(TaxReportComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load report data and auto-select the most recent year', async () => {
    expect(component.isLoading()).toBe(false);
    expect(component.reportCurrency()).toBe('EUR');
    expect(component.selectedYear()).toBe(2024); // most recent first
    expect(component.yearSummaries()).toEqual(mockYearSummaries);
  });

  // ── Year selection ────────────────────────────────────────────────────

  it('selectYear() should update selectedYear', async () => {
    component.selectYear(2023);
    expect(component.selectedYear()).toBe(2023);
  });

  it('clearYearFilter() should set selectedYear to null', async () => {
    component.clearYearFilter();
    expect(component.selectedYear()).toBeNull();
  });

  // ── filteredTransactions computed ─────────────────────────────────────

  it('filteredTransactions() should filter by selected year', async () => {
    component.selectYear(2023);
    await fixture.whenStable();
    const filtered = component.filteredTransactions();
    expect(filtered.length).toBe(1);
    expect(filtered[0].transaction.id).toBe(2);
  });

  it('filteredTransactions() should return all when selectedYear is null', async () => {
    component.clearYearFilter();
    await fixture.whenStable();
    expect(component.filteredTransactions().length).toBe(2);
  });

  // ── KPIs computed ─────────────────────────────────────────────────────

  it('selectedYearKPIs() should return correct values for selected year', async () => {
    component.selectYear(2024);
    await fixture.whenStable();
    const kpis = component.selectedYearKPIs();
    expect(kpis.totalGains).toBe(5000);
    expect(kpis.totalLosses).toBe(-2000);
    expect(kpis.netPL).toBe(3000);
    expect(kpis.disallowed).toBe(500);
  });

  it('selectedYearKPIs() should aggregate all years when selectedYear is null', async () => {
    component.clearYearFilter();
    await fixture.whenStable();
    const kpis = component.selectedYearKPIs();
    expect(kpis.totalGains).toBe(8000); // 5000 + 3000
    expect(kpis.totalLosses).toBe(-3000); // -2000 + -1000
    expect(kpis.disallowed).toBe(500);
  });

  // ── Utility methods ───────────────────────────────────────────────────

  it('getSpotPrice() should return EUR price when reportCurrency is EUR', async () => {
    const tx = makeTx(99, '2024-01-01T00:00:00Z');
    tx.transaction.spotPriceEUR = 40000;
    tx.transaction.spotPriceUSD = 45000;
    expect(component.getSpotPrice(tx)).toBe(40000);
  });

  it('getEventIcons() should return correct icons for known types', () => {
    expect(component.getEventIcons('SWAP')).toEqual({ fromIcon: 'sell', toIcon: 'shopping_cart' });
    expect(component.getEventIcons('DEPOSIT')).toEqual({ fromIcon: '', toIcon: 'south_east' });
  });

  it('getEventIcons() should return fallback icons for unknown types', () => {
    expect(component.getEventIcons('UNKNOWN')).toEqual({ fromIcon: 'swap_horiz', toIcon: 'swap_horiz' });
  });
});
