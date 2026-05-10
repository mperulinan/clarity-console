import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { AssetSelectorComponent } from './asset-selector.component';
import { PortfolioService } from '../../../services/portfolio.service';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of, throwError } from 'rxjs';
import { AssetDto } from '../../../models/asset';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { MatAutocompleteHarness } from '@angular/material/autocomplete/testing';
import { MatInputHarness } from '@angular/material/input/testing';
import { HarnessLoader } from '@angular/cdk/testing';

describe('AssetSelectorComponent', () => {
  let component: AssetSelectorComponent;
  let fixture: ComponentFixture<AssetSelectorComponent>;
  let portfolioServiceSpy: any;
  let loader: HarnessLoader;

  const mockAsset: AssetDto = {
    id: '1',
    symbol: 'BTC',
    name: 'Bitcoin',
    type: 'Crypto',
    transactionCount: 0
  };

  beforeEach(async () => {
    // Basic mock using Vitest vi.fn() or simple jasmine-like object
    portfolioServiceSpy = {
      searchAssets: (query: string) => of([mockAsset]),
      syncAsset: (asset: AssetDto) => of(asset)
    };

    await TestBed.configureTestingModule({
      imports: [AssetSelectorComponent, NoopAnimationsModule],
      providers: [
        { provide: PortfolioService, useValue: portfolioServiceSpy }
      ]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(AssetSelectorComponent);
    component = fixture.componentInstance;
    loader = TestbedHarnessEnvironment.loader(fixture);
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display the initial value if provided', async () => {
    fixture.componentRef.setInput('value', mockAsset);
    fixture.detectChanges();
    await new Promise(r => setTimeout(r, 350));
    expect(component.searchControl.value).toEqual(mockAsset);
  });

  it('should clear selection when input is blurred and is a string', async () => {
    fixture.componentRef.setInput('value', mockAsset);
    fixture.detectChanges();
    await new Promise(r => setTimeout(r, 50));
    
    component.searchControl.setValue('invalid text');
    component.onAssetInputBlur();
    
    expect(component.selectedAsset()).toBeNull();
    expect(component.searchControl.value).toBeNull();
  });

  it('should emit assetChange when an asset is selected via autocomplete', async () => {
    let emittedAsset: AssetDto | null = null;
    component.assetChange.subscribe(val => emittedAsset = val);
    
    fixture.detectChanges();
    
    // Simulate user typing
    component.searchControl.setValue('Bit');
    await new Promise(r => setTimeout(r, 350)); // Wait for debounceTime
    fixture.detectChanges();
    
    // Simulate option selected
    component.onAssetSelected({ option: { value: mockAsset } } as any);
    await new Promise(r => setTimeout(r, 50));
    
    expect(emittedAsset).toEqual(mockAsset);
    expect(component.selectedAsset()).toEqual(mockAsset);
  });

  it('should disable input when disabled input is true', async () => {
    fixture.componentRef.setInput('disabled', true);
    fixture.detectChanges();
    await new Promise(r => setTimeout(r, 50));
    
    expect(component.searchControl.disabled).toBe(true);
  });
});
