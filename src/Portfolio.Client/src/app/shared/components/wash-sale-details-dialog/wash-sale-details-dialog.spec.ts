import { ComponentFixture, TestBed } from '@angular/core/testing';
import { WashSaleDetailsDialogComponent, WashSaleDialogData } from './wash-sale-details-dialog';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { ProcessedTransaction } from '../../../models/transaction';

describe('WashSaleDetailsDialogComponent', () => {
  let component: WashSaleDetailsDialogComponent;
  let fixture: ComponentFixture<WashSaleDetailsDialogComponent>;
  let mockDialogRef: any;

  const mockLossTx: ProcessedTransaction = {
    transaction: {
      id: 1, type: { value: 'SELL', name: 'Sell', requiresFromAsset: true, requiresToAsset: false },
      amountSpent: 1, amountReceived: 0, fee: 0, date: new Date().toISOString(),
      spotPriceEUR: 10, spotPriceUSD: 11
    },
    profitLoss: -11,
    disallowsPreviousLosses: [],
    isLossDisallowed: false
  };

  const mockData: WashSaleDialogData = {
    lossTx: mockLossTx,
    repurchaseTx: { ...mockLossTx, transaction: { ...mockLossTx.transaction, type: { value: 'BUY', name: 'Buy', requiresFromAsset: false, requiresToAsset: true }, id: 2 } },
    currency: 'USD'
  };

  beforeEach(async () => {
    mockDialogRef = { close: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [WashSaleDetailsDialogComponent],
      providers: [
        { provide: MatDialogRef, useValue: mockDialogRef },
        { provide: MAT_DIALOG_DATA, useValue: mockData }
      ]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(WashSaleDetailsDialogComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should return correct spot price based on currency', () => {
    expect(component.getSpotPrice(mockLossTx)).toBe(11); // USD
    
    component.data.currency = 'EUR';
    expect(component.getSpotPrice(mockLossTx)).toBe(10); // EUR
  });

  it('should close dialog on close()', () => {
    component.close();
    expect(mockDialogRef.close).toHaveBeenCalled();
  });
});
