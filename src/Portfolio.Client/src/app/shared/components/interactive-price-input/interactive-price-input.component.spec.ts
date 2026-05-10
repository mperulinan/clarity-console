import { ComponentFixture, TestBed } from '@angular/core/testing';
import { InteractivePriceInputComponent, PriceInputMode } from './interactive-price-input.component';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { By } from '@angular/platform-browser';

describe('InteractivePriceInputComponent', () => {
  let component: InteractivePriceInputComponent;
  let fixture: ComponentFixture<InteractivePriceInputComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InteractivePriceInputComponent, NoopAnimationsModule]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(InteractivePriceInputComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should calculate total correctly in unit mode', async () => {
    fixture.componentRef.setInput('assetAmount', 2);
    fixture.componentRef.setInput('value', 100);
    await fixture.whenStable();

    expect(component.calculatedTotal()).toBe(200);
  });

  it('should emit updated value when typing in unit mode', async () => {
    let emittedValue: number | null = null;
    component.valueChange.subscribe(v => emittedValue = v);

    await fixture.whenStable();
    
    component.internalControl.setValue(150);
    expect(emittedValue).toBe(150);
  });

  it('should emit correct unit price when typing in total mode', async () => {
    let emittedValue: number | null = null;
    component.valueChange.subscribe(v => emittedValue = v);

    fixture.componentRef.setInput('assetAmount', 2);
    await fixture.whenStable();
    
    component.toggleMode(); // switch to total mode
    component.internalControl.setValue(300); // User types 300 total
    
    expect(emittedValue).toBe(150); // Unit price is 150
  });

  it('should disable internal control when disabled input is true', async () => {
    fixture.componentRef.setInput('disabled', true);
    await fixture.whenStable();
    
    expect(component.internalControl.disabled).toBe(true);
  });
});
