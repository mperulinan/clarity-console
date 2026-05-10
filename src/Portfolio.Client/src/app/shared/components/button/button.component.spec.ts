import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ButtonComponent } from './button.component';

describe('ButtonComponent', () => {
  let component: ButtonComponent;
  let fixture: ComponentFixture<ButtonComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ButtonComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(ButtonComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should set default host classes', async () => {
    await fixture.whenStable();
    expect(fixture.nativeElement.className).toContain('btn-text');
    expect(fixture.nativeElement.className).toContain('color-primary');
    expect(fixture.nativeElement.className).toContain('size-md');
    expect(fixture.nativeElement.className).toContain('justify-center');
  });

  it('should update host classes when inputs change', async () => {
    fixture.componentRef.setInput('variant', 'filled');
    fixture.componentRef.setInput('color', 'error');
    fixture.componentRef.setInput('size', 'lg');
    fixture.componentRef.setInput('justify', 'start');
    
    await fixture.whenStable();
    
    expect(fixture.nativeElement.className).toContain('btn-filled');
    expect(fixture.nativeElement.className).toContain('color-error');
    expect(fixture.nativeElement.className).toContain('size-lg');
    expect(fixture.nativeElement.className).toContain('justify-start');
  });

  it('should set disabled attribute', async () => {
    fixture.componentRef.setInput('disabled', true);
    await fixture.whenStable();
    expect(fixture.nativeElement.getAttribute('disabled')).toBeTruthy();
  });

  it('should not set disabled attribute when false', async () => {
    fixture.componentRef.setInput('disabled', false);
    await fixture.whenStable();
    expect(fixture.nativeElement.getAttribute('disabled')).toBeFalsy();
  });
});
