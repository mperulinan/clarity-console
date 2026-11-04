import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TransactionFormComponent } from './new-transaction.component';
import { PortfolioService } from '../../services/portfolio.service';
import { Router, ActivatedRoute } from '@angular/router';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { HttpErrorResponse } from '@angular/common/http';
import { of, throwError } from 'rxjs';
import { TransactionType } from '../../models/transaction';
import { AssetDto } from '../../models/asset';
import { provideNativeDateAdapter } from '@angular/material/core';
import { CurrencyService } from '../../services/currency.service';
import { signal } from '@angular/core';

/** Convenience alias keeps model.update() callbacks typed without verbosity. */
type FormModel = ReturnType<TransactionFormComponent['model']>;

describe('TransactionFormComponent', () => {
  let component: TransactionFormComponent;
  let fixture: ComponentFixture<TransactionFormComponent>;
  let portfolioServiceSpy: any;
  let routerSpy: any;
  let activatedRouteStub: any;

  const mockTypes: TransactionType[] = [
    { value: 'BUY',  name: 'Buy',  requiresFromAsset: false, requiresToAsset: true,  requiresSpotPrice: true  },
    { value: 'SELL', name: 'Sell', requiresFromAsset: true,  requiresToAsset: false, requiresSpotPrice: true  },
    { value: 'SWAP', name: 'Swap', requiresFromAsset: true,  requiresToAsset: true,  requiresSpotPrice: true  },
  ];

  const mockFiat: AssetDto = { id: 'usd-1', symbol: 'USD', name: 'US Dollar', type: 'Fiat', transactionCount: 0 };

  const mockAsset: AssetDto = {
    id: 'btc-1', symbol: 'BTC', name: 'Bitcoin', type: 'Crypto', transactionCount: 0
  };

  const mockCurrencyService = {
    supportedCurrencies: signal<AssetDto[]>([mockFiat]),
    taxCurrency:          signal<AssetDto | null>(mockFiat),
    defaultCurrency:      signal<AssetDto | null>(mockFiat),
  };

  beforeEach(async () => {
    portfolioServiceSpy = {
      getTransactionTypes: () => of(mockTypes),
      addTransaction:      vi.fn().mockReturnValue(of({ id: 99 })),
      updateTransaction:   vi.fn().mockReturnValue(of(null)),
      getTaxCurrency:      () => of(mockFiat),
      getSpotPrice:        vi.fn().mockReturnValue(of(0)),
    };
    routerSpy = { navigate: vi.fn() };

    activatedRouteStub = {
      snapshot: {
        paramMap: {
          get: vi.fn().mockReturnValue(null)
        }
      }
    };

    await TestBed.configureTestingModule({
      imports: [TransactionFormComponent, NoopAnimationsModule],
      providers: [
        { provide: PortfolioService,  useValue: portfolioServiceSpy },
        { provide: Router,            useValue: routerSpy },
        { provide: ActivatedRoute,    useValue: activatedRouteStub },
        { provide: CurrencyService,   useValue: mockCurrencyService },
        provideNativeDateAdapter()
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(TransactionFormComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create and render stepper', () => {
    expect(component).toBeTruthy();
    fixture.detectChanges();
    const stepper = fixture.nativeElement.querySelector('mat-stepper');
    expect(stepper).toBeTruthy();
  });

  it('should load transaction types on init and pre-select the first', async () => {
    expect(component.transactionTypes()).toEqual(mockTypes);
    expect(component.model().type).toBe('BUY');
    expect(component.isLoadingTypes()).toBe(false);
  });

  // ── Step validity tests ───────────────────────────────────────────────

  it('isStep1Valid should be false if no type is selected', async () => {
    component.model.update((m: FormModel) => ({ ...m, type: '' }));
    await fixture.whenStable();
    expect(component.isStep1Valid()).toBe(false);
  });

  it('isStep1Valid should be false for BUY if toAsset or amountReceived is missing', async () => {
    component.model.update((m: FormModel) => ({ ...m, type: 'BUY', amountReceived: 0 }));
    component.toAsset.set(null);
    await fixture.whenStable();
    expect(component.isStep1Valid()).toBe(false);
  });

  it('isStep1Valid should be true for BUY when toAsset and amountReceived are set', async () => {
    component.model.update((m: FormModel) => ({ ...m, type: 'BUY', amountReceived: 1 }));
    component.toAsset.set(mockAsset);
    await fixture.whenStable();
    expect(component.isStep1Valid()).toBe(true);
  });

  it('isStep1Valid should be true for SELL when fromAsset and amountSpent are set', async () => {
    component.model.update((m: FormModel) => ({ ...m, type: 'SELL', amountSpent: 1 }));
    await fixture.whenStable();
    component.fromAsset.set(mockAsset);
    await fixture.whenStable();
    expect(component.isStep1Valid()).toBe(true);
  });

  // ── Step 2 validity tests ─────────────────────────────────────────────

  it('isStep2Valid should be false when spotPrice is 0 and no fiat leg', async () => {
    component.toAsset.set(mockAsset);
    component.model.update((m: FormModel) => ({ ...m, type: 'BUY', amountReceived: 1, spotPrice: 0 }));
    fixture.detectChanges();
    await fixture.whenStable();
    expect(component.isStep1Valid()).toBe(true);
    expect(component.isStep2Valid()).toBe(false);
  });

  it('isStep2Valid should be true when step 1 is valid and spotPrice is set', async () => {
    component.toAsset.set(mockAsset);
    component.model.update((m: FormModel) => ({ ...m, type: 'BUY', amountReceived: 1 }));
    fixture.detectChanges();
    await fixture.whenStable();
    component.model.update((m: FormModel) => ({ ...m, spotPrice: 100 }));
    fixture.detectChanges();
    await fixture.whenStable();
    expect(component.isStep1Valid()).toBe(true);
    expect(component.isStep2Valid()).toBe(true);
  });

  // ── Computed label tests ──────────────────────────────────────────────

  it('pricedAssetSymbol should reflect the selected SELL asset', async () => {
    component.model.update((m: FormModel) => ({ ...m, type: 'SELL' }));
    await fixture.whenStable();
    component.fromAsset.set(mockAsset);
    await fixture.whenStable();
    expect(component.pricedAssetSymbol()).toBe('BTC');
  });

  it('pricedAssetSymbol should reflect the selected BUY asset', async () => {
    component.model.update((m: FormModel) => ({ ...m, type: 'BUY' }));
    component.toAsset.set(mockAsset);
    await fixture.whenStable();
    expect(component.pricedAssetSymbol()).toBe('BTC');
  });

  // ── Cancel / Submit ────────────────────────────────────────────────────

  it('onCancel() should navigate to /', () => {
    component.onCancel();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/']);
  });

  // ── Spot Price Fetching ────────────────────────────────────────────────

  it('onStep1Next() should fetch spot price and round to 2 decimals', async () => {
    portfolioServiceSpy.getSpotPrice = vi.fn().mockReturnValue(of(1234.56789));

    component.model.update((m: FormModel) => ({ ...m, type: 'BUY', amountReceived: 1, spotPriceCurrency: 'USD' as any }));
    component.toAsset.set(mockAsset);
    fixture.detectChanges();
    await fixture.whenStable();

    component.onStep1Next();
    await fixture.whenStable();

    expect(portfolioServiceSpy.getSpotPrice).toHaveBeenCalled();
    expect(component.fetchedSpotPrice()).toBe(1234.57);
  });

  it('should show server validation message on 400 from addTransaction', async () => {
    portfolioServiceSpy.addTransaction = () =>
      throwError(() =>
        new HttpErrorResponse({
          status: 400,
          error: { message: 'FromAssetId is required for Swap' },
        })
      );

    component.toAsset.set(mockAsset);
    component.model.update((m: FormModel) => ({ ...m, type: 'SWAP', amountSpent: 1, amountReceived: 1, spotPrice: 10 }));
    component.fromAsset.set(mockAsset);
    fixture.detectChanges();
    await fixture.whenStable();
    component.model.update((m: FormModel) => ({ ...m, spotPrice: 10 }));

    await component['saveTransaction']();
    await fixture.whenStable();

    expect(component.submitError()).toBe('FromAssetId is required for Swap');
    expect(component.isSubmitting()).toBe(false);
  });

  it('should show network message when addTransaction cannot reach the server', async () => {
    portfolioServiceSpy.addTransaction = () =>
      throwError(() => new HttpErrorResponse({ status: 0 }));

    await component['saveTransaction']();
    await fixture.whenStable();

    expect(component.submitError()).toContain('Could not reach the server');
    expect(component.isSubmitting()).toBe(false);
  });

  it('saveTransaction() should navigate on successful creation', async () => {
    component.toAsset.set(mockAsset);
    component.model.update((m: FormModel) => ({ ...m, type: 'BUY', amountReceived: 1, spotPrice: 100 }));
    await component['saveTransaction']();
    expect(portfolioServiceSpy.addTransaction).toHaveBeenCalled();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/transactions'], { state: { updatedTransactionId: 99 } });
  });

  it('saveTransaction() should navigate on successful update', async () => {
    component.transactionId.set(42);
    component.toAsset.set(mockAsset);
    component.model.update((m: FormModel) => ({ ...m, type: 'BUY', amountReceived: 1, spotPrice: 100 }));
    await component['saveTransaction']();
    expect(portfolioServiceSpy.updateTransaction).toHaveBeenCalledWith(42, expect.anything());
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/transactions'], { state: { updatedTransactionId: 42 } });
  });

  it('onSubmitInvalid() should set submitError message', () => {
    component['onSubmitInvalid']();
    expect(component.submitError()).toBeTruthy();
  });

  // ── Form Population (edit mode) ────────────────────────────────────────

  it('should uppercase fiat currency strings when populating form from an existing transaction', async () => {
    portfolioServiceSpy.getTransaction = vi.fn().mockReturnValue(of({
        id: 1,
        date: new Date().toISOString(),
        type: mockTypes[0],
        amountSpent: 100,
        amountReceived: 2,
        spotPriceUSD: 50,
        spotPriceInputCurrency: 'usd',    // from backend — should be uppercased
        fee: 0,
        feePriceInputCurrency: 'eur'      // from backend — should be uppercased
    }));

    // Simulate route param returning an id
    activatedRouteStub.snapshot.paramMap.get.mockReturnValue('1');
    await component['checkEditMode']();
    await fixture.whenStable();

    expect(portfolioServiceSpy.getTransaction).toHaveBeenCalledWith(1);

    const m = component.model();
    expect(m.spotPriceCurrency).toBe('USD');
    expect(m.feeSpotPriceCurrency).toBe('EUR');
  });
});
