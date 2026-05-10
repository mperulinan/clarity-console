import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BadgeComponent } from './badge.component';

describe('BadgeComponent', () => {
  let component: BadgeComponent;
  let fixture: ComponentFixture<BadgeComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BadgeComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(BadgeComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should set default host classes', async () => {
    await fixture.whenStable();
    expect(fixture.nativeElement.className).toContain('badge-neutral');
    expect(fixture.nativeElement.className).toContain('size-md');
  });

  it('should update host classes when inputs change', async () => {
    fixture.componentRef.setInput('semantic', 'gain');
    fixture.componentRef.setInput('size', 'sm');
    
    await fixture.whenStable();
    
    expect(fixture.nativeElement.className).toContain('badge-gain');
    expect(fixture.nativeElement.className).toContain('size-sm');
  });
});
