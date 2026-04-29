import { TestBed } from '@angular/core/testing';
import { App } from './app';
import { provideRouter } from '@angular/router';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideRouter([])
      ]
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should render the sidebar navigation', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const nav = fixture.nativeElement.querySelector('nav.sidebar');
    expect(nav).toBeTruthy();
  });

  it('should start with sidebar collapsed', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const app = fixture.componentInstance;
    expect(app.sidebarExpanded()).toBeFalsy();
  });

  it('should toggle sidebar when toggleSidebar is called', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const app = fixture.componentInstance;
    app.toggleSidebar();
    expect(app.sidebarExpanded()).toBeTruthy();
    app.toggleSidebar();
    expect(app.sidebarExpanded()).toBeFalsy();
  });

  it('should expose portfolio nav items', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app.portfolioItems().length).toBeGreaterThan(0);
  });
});
