import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NewTransactionComponent } from './new-transaction.component';
import { PortfolioService } from '../../services/portfolio.service';
import { Router } from '@angular/router';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { HttpErrorResponse } from '@angular/common/http';
import { of, throwError, firstValueFrom } from 'rxjs';
import { TransactionType } from '../../models/transaction';
import { AssetDto } from '../../models/asset';
import { provideNativeDateAdapter } from '@angular/material/core';

describe('NewTransactionComponent', () => {
  let component: NewTransactionComponent;
  let fixture: ComponentFixture<NewTransactionComponent>;
  let portfolioServiceSpy: any;
  let routerSpy: any;

  const mockTypes: TransactionType[] = [
    { value: 'BUY', name: 'Buy', requiresFromAsset: false, requiresToAsset: true },
    { value: 'SELL', name: 'Sell', requiresFromAsset: true, requiresToAsset: false },
    { value: 'SWAP', name: 'Swap', requiresFromAsset: true, requiresToAsset: true },
  ];

  const mockFiats: AssetDto[] = [
    { id: 'usd-1', symbol: 'USD', name: 'US Dollar', type: 'Fiat', transactionCount: 0 }
  ];

  const mockAsset: AssetDto = {
    id: 'btc-1', symbol: 'BTC', name: 'Bitcoin', type: 'Crypto', transactionCount: 0
  };

  beforeEach(async () => {
    portfolioServiceSpy = {
      getTransactionTypes: () => of(mockTypes),
      getFiatCurrencies: () => of(mockFiats),
      addTransaction: () => of(void 0),
      getTaxCurrency: () => of(mockFiats[0])
    };
    routerSpy = { navigate: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [NewTransactionComponent, NoopAnimationsModule],
      providers: [
        { provide: PortfolioService, useValue: portfolioServiceSpy },
        { provide: Router, useValue: routerSpy },
        provideNativeDateAdapter()
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(NewTransactionComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load transaction types on init and pre-select the first', async () => {
    expect(component.transactionTypes()).toEqual(mockTypes);
    expect(component.model().type).toBe('BUY');
    expect(component.isLoadingTypes()).toBe(false);
  });

  it('should load fiat currencies on init', async () => {
    expect(component.fiatCurrencies()).toEqual(mockFiats);
  });

  // ── Step validity tests ───────────────────────────────────────────────

  it('isStep1Valid should be false if no type is selected', async () => {
    component.model.update(m => ({ ...m, type: '' }));
    await fixture.whenStable();
    expect(component.isStep1Valid()).toBe(false);
  });

  it('isStep1Valid should be false for BUY if toAsset or amountReceived is missing', async () => {
    // BUY requires toAsset + amountReceived
    component.model.update(m => ({ ...m, type: 'BUY', amountReceived: 0 }));
    component.toAsset.set(null);
    await fixture.whenStable();
    expect(component.isStep1Valid()).toBe(false);
  });

  it('isStep1Valid should be true for BUY when toAsset and amountReceived are set', async () => {
    component.model.update(m => ({ ...m, type: 'BUY', amountReceived: 1 }));
    component.toAsset.set(mockAsset);
    await fixture.whenStable();
    expect(component.isStep1Valid()).toBe(true);
  });

  it('isStep1Valid should be true for SELL when fromAsset and amountSpent are set', async () => {
    component.model.update(m => ({ ...m, type: 'SELL', amountSpent: 1 }));
    component.fromAsset.set(mockAsset);
    await fixture.whenStable();
    expect(component.isStep1Valid()).toBe(true);
  });

  // ── Step 2 validity tests ─────────────────────────────────────────────

  it('isStep2Valid should be false when spotPrice is 0 and no fiat leg', async () => {
    component.toAsset.set(mockAsset);
    component.model.update(m => ({ ...m, type: 'BUY', amountReceived: 1, spotPrice: 0 }));
    fixture.detectChanges();
    await fixture.whenStable();
    expect(component.isStep1Valid()).toBe(true);
    expect(component.isStep2Valid()).toBe(false);
  });

  it('isStep2Valid should be true when step 1 is valid and spotPrice is set', async () => {
    component.toAsset.set(mockAsset);
    component.model.update(m => ({ ...m, type: 'BUY', amountReceived: 1 }));
    fixture.detectChanges();
    await fixture.whenStable();
    component.model.update(m => ({ ...m, spotPrice: 100 }));
    fixture.detectChanges();
    await fixture.whenStable();
    expect(component.isStep1Valid()).toBe(true);
    expect(component.isStep2Valid()).toBe(true);
  });

  // ── Computed label tests ──────────────────────────────────────────────

  it('pricedAssetSymbol should reflect the selected SELL asset', async () => {
    component.model.update(m => ({ ...m, type: 'SELL' }));
    component.fromAsset.set(mockAsset);
    await fixture.whenStable();
    expect(component.pricedAssetSymbol()).toBe('BTC');
  });

  it('pricedAssetSymbol should reflect the selected BUY asset', async () => {
    component.model.update(m => ({ ...m, type: 'BUY' }));
    component.toAsset.set(mockAsset);
    await fixture.whenStable();
    expect(component.pricedAssetSymbol()).toBe('BTC');
  });

  // ── Cancel / Submit ────────────────────────────────────────────────────

  it('onCancel() should navigate to /', () => {
    component.onCancel();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/']);
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
    component.model.update(m => ({ ...m, type: 'SWAP', amountSpent: 1, amountReceived: 1, spotPrice: 10 }));
    component.fromAsset.set(mockAsset);
    fixture.detectChanges();
    await fixture.whenStable();
    component.model.update(m => ({ ...m, spotPrice: 10 }));

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
});
