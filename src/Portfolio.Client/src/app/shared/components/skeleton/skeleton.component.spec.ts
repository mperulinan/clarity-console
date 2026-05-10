import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SkeletonComponent } from './skeleton.component';

describe('SkeletonComponent', () => {
  let component: SkeletonComponent;
  let fixture: ComponentFixture<SkeletonComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SkeletonComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(SkeletonComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should set default host bindings', async () => {
    await fixture.whenStable();
    const el = fixture.nativeElement;
    
    expect(el.className).toContain('skel-line');
    expect(el.getAttribute('aria-hidden')).toBe('true');
    expect(el.getAttribute('aria-busy')).toBe('true');
  });

  it('should update host classes and styles when inputs change', async () => {
    fixture.componentRef.setInput('type', 'bento');
    fixture.componentRef.setInput('width', '100px');
    fixture.componentRef.setInput('height', '50px');
    
    await fixture.whenStable();
    
    const el = fixture.nativeElement;
    expect(el.className).toContain('skel-bento');
    expect(el.style.width).toBe('100px');
    expect(el.style.height).toBe('50px');
  });
});
