import { ComponentFixture, TestBed } from '@angular/core/testing';
import { EmptyStateComponent } from './empty-state.component';
import { MatIconModule } from '@angular/material/icon';
import { By } from '@angular/platform-browser';

describe('EmptyStateComponent', () => {
  let component: EmptyStateComponent;
  let fixture: ComponentFixture<EmptyStateComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EmptyStateComponent, MatIconModule]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(EmptyStateComponent);
    component = fixture.componentInstance;
  });

  it('should create with required inputs', async () => {
    fixture.componentRef.setInput('icon', 'search_off');
    fixture.componentRef.setInput('message', 'No results found');
    await fixture.whenStable();

    expect(component).toBeTruthy();
    
    const iconEl = fixture.debugElement.query(By.css('mat-icon'));
    expect(iconEl.nativeElement.textContent.trim()).toBe('search_off');
    
    // Message rendering test depends on HTML structure, but we can verify text presence
    expect(fixture.nativeElement.textContent).toContain('No results found');
  });

  it('should display subMessage when provided', async () => {
    fixture.componentRef.setInput('icon', 'search_off');
    fixture.componentRef.setInput('message', 'No results found');
    fixture.componentRef.setInput('subMessage', 'Try adjusting your filters');
    
    await fixture.whenStable();
    
    expect(fixture.nativeElement.textContent).toContain('Try adjusting your filters');
  });
});
