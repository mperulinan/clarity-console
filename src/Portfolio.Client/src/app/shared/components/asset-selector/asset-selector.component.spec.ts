import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AssetSelectorComponent } from './asset-selector.component';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { AssetDto } from '../../../models/asset';
import { MatDialog } from '@angular/material/dialog';
import { By } from '@angular/platform-browser';

describe('AssetSelectorComponent', () => {
  let component: AssetSelectorComponent;
  let fixture: ComponentFixture<AssetSelectorComponent>;
  let dialogSpy: any;

  const mockAsset: AssetDto = {
    id: '1',
    symbol: 'BTC',
    name: 'Bitcoin',
    type: 'Crypto',
    transactionCount: 0
  };

  beforeEach(async () => {
    dialogSpy = {
      open: vi.fn().mockReturnValue({
        afterClosed: () => of(undefined)
      })
    };

    await TestBed.configureTestingModule({
      imports: [AssetSelectorComponent, NoopAnimationsModule]
    })
    .overrideComponent(AssetSelectorComponent, {
      set: {
        providers: [
          { provide: MatDialog, useValue: dialogSpy }
        ]
      }
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(AssetSelectorComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  it('should display placeholder when no value is selected', () => {
    fixture.componentRef.setInput('placeholder', 'Select Asset');
    fixture.componentRef.setInput('value', null);
    fixture.detectChanges();

    const placeholderEl = fixture.debugElement.query(By.css('.selector-trigger__placeholder'));
    expect(placeholderEl).toBeTruthy();
    expect(placeholderEl.nativeElement.textContent.trim()).toBe('Select Asset');

    const chipEl = fixture.debugElement.query(By.css('.asset-chip'));
    expect(chipEl).toBeFalsy();
  });

  it('should display the asset chip when value is provided', () => {
    fixture.componentRef.setInput('value', mockAsset);
    fixture.componentRef.setInput('chipClass', 'custom-chip');
    fixture.detectChanges();

    const chipEl = fixture.debugElement.query(By.css('.custom-chip'));
    expect(chipEl).toBeTruthy();

    const symbolEl = fixture.debugElement.query(By.css('.asset-chip__symbol'));
    expect(symbolEl.nativeElement.textContent.trim()).toBe('BTC');

    const nameEl = fixture.debugElement.query(By.css('.asset-chip__name'));
    expect(nameEl.nativeElement.textContent.trim()).toBe('Bitcoin');

    const placeholderEl = fixture.debugElement.query(By.css('.selector-trigger__placeholder'));
    expect(placeholderEl).toBeFalsy();
  });

  it('should emit assetChange with null when clearAsset is clicked', () => {
    fixture.componentRef.setInput('value', mockAsset);
    fixture.detectChanges();

    let emittedValue: AssetDto | null | undefined = undefined;
    component.assetChange.subscribe(val => emittedValue = val);

    const clearButton = fixture.debugElement.query(By.css('.asset-chip__clear'));
    expect(clearButton).toBeTruthy();
    
    clearButton.nativeElement.click();
    expect(emittedValue).toBeNull();
  });

  it('should open dialog when trigger is clicked and not disabled', () => {
    fixture.componentRef.setInput('value', null);
    fixture.componentRef.setInput('disabled', false);
    fixture.detectChanges();

    const trigger = fixture.debugElement.query(By.css('.selector-trigger'));
    expect(trigger).toBeTruthy();

    trigger.nativeElement.click();

    expect(dialogSpy.open).toHaveBeenCalled();
  });

  it('should emit assetChange when dialog returns a value', () => {
    const selectedAsset: AssetDto = {
      id: '2',
      symbol: 'ETH',
      name: 'Ethereum',
      type: 'Crypto',
      transactionCount: 0
    };

    dialogSpy.open.mockReturnValue({
      afterClosed: () => of(selectedAsset)
    });

    fixture.componentRef.setInput('value', null);
    fixture.detectChanges();

    let emittedValue: AssetDto | null | undefined = undefined;
    component.assetChange.subscribe(val => emittedValue = val);

    const trigger = fixture.debugElement.query(By.css('.selector-trigger'));
    trigger.nativeElement.click();

    expect(emittedValue).toEqual(selectedAsset);
  });

  it('should not open dialog when trigger is clicked and disabled is true', () => {
    fixture.componentRef.setInput('value', null);
    fixture.componentRef.setInput('disabled', true);
    fixture.detectChanges();

    const trigger = fixture.debugElement.query(By.css('.selector-trigger'));
    expect(trigger).toBeTruthy();

    trigger.nativeElement.click();

    expect(dialogSpy.open).not.toHaveBeenCalled();
  });
});
