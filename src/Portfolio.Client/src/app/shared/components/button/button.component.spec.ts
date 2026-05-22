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

  function nativeButton(): HTMLButtonElement {
    return fixture.nativeElement.querySelector('button')!;
  }

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should set default classes on the native button', async () => {
    await fixture.whenStable();
    const btn = nativeButton();
    expect(btn.className).toContain('btn-text');
    expect(btn.className).toContain('color-primary');
    expect(btn.className).toContain('size-md');
    expect(btn.className).toContain('justify-center');
  });

  it('should update button classes when inputs change', async () => {
    fixture.componentRef.setInput('variant', 'filled');
    fixture.componentRef.setInput('color', 'error');
    fixture.componentRef.setInput('size', 'lg');
    fixture.componentRef.setInput('justify', 'start');
    
    await fixture.whenStable();
    
    const btn = nativeButton();
    expect(btn.className).toContain('btn-filled');
    expect(btn.className).toContain('color-error');
    expect(btn.className).toContain('size-lg');
    expect(btn.className).toContain('justify-start');
  });

  it('should disable the native button', async () => {
    fixture.componentRef.setInput('disabled', true);
    await fixture.whenStable();
    expect(nativeButton().disabled).toBe(true);
  });

  it('should forward type to the native button', async () => {
    fixture.componentRef.setInput('type', 'submit');
    await fixture.whenStable();
    expect(nativeButton().type).toBe('submit');
  });
});
