import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AssetSearchDialogComponent } from './asset-search-dialog.component';
import { PortfolioService } from '../../../services/portfolio.service';
import { MatDialogRef } from '@angular/material/dialog';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of, throwError } from 'rxjs';
import { AssetDto } from '../../../models/asset';

describe('AssetSearchDialogComponent', () => {
  let component: AssetSearchDialogComponent;
  let fixture: ComponentFixture<AssetSearchDialogComponent>;
  let portfolioServiceSpy: any;
  let dialogRefSpy: any;

  const mockAsset: AssetDto = {
    id: 'btc-1',
    name: 'Bitcoin',
    symbol: 'BTC',
    type: 'Crypto',
    imageUrl: 'btc.png',
    transactionCount: 1
  };

  beforeEach(async () => {
    portfolioServiceSpy = {
      searchAssets: vi.fn().mockReturnValue(of([mockAsset])),
      syncAsset: vi.fn().mockReturnValue(of(mockAsset))
    };
    dialogRefSpy = { close: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [AssetSearchDialogComponent, NoopAnimationsModule],
      providers: [
        { provide: PortfolioService, useValue: portfolioServiceSpy },
        { provide: MatDialogRef, useValue: dialogRefSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AssetSearchDialogComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('close() should close dialogRef', () => {
    component.close();
    expect(dialogRefSpy.close).toHaveBeenCalled();
  });

  it('selectAsset() should close dialog with asset if sync is not needed', () => {
    component.selectAsset(mockAsset);
    expect(dialogRefSpy.close).toHaveBeenCalledWith(mockAsset);
  });

  it('selectAsset() should sync and close if asset has no id or no image', () => {
    const unsyncedAsset: AssetDto = { ...mockAsset, id: '', imageUrl: undefined };
    component.selectAsset(unsyncedAsset);
    expect(portfolioServiceSpy.syncAsset).toHaveBeenCalledWith(unsyncedAsset);
    expect(dialogRefSpy.close).toHaveBeenCalledWith(mockAsset);
  });

  it('selectAsset() should handle sync error and set syncError message', () => {
    portfolioServiceSpy.syncAsset = vi.fn().mockReturnValue(throwError(() => new Error('Sync failed')));
    const unsyncedAsset: AssetDto = { ...mockAsset, id: '', imageUrl: undefined };
    component.selectAsset(unsyncedAsset);
    expect(component.syncError()).toBe('Could not sync this asset right now. Please try again.');
  });


});
