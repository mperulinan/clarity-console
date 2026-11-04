import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AssetAvatarComponent } from './asset-avatar.component';

describe('AssetAvatarComponent', () => {
  let component: AssetAvatarComponent;
  let fixture: ComponentFixture<AssetAvatarComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AssetAvatarComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(AssetAvatarComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should compute avatarSize correctly', () => {
    fixture.componentRef.setInput('size', 48);
    expect(component.avatarSize).toBe('48px');
  });

  it('initials should return first 2 chars of symbol', () => {
    fixture.componentRef.setInput('symbol', 'BTC');
    expect(component.initials).toBe('BT');
  });

  it('initials should return first 2 chars of name if symbol is single char or missing', () => {
    fixture.componentRef.setInput('symbol', '');
    fixture.componentRef.setInput('name', 'Ethereum');
    expect(component.initials).toBe('ET');
  });

  it('initials should return uppercase symbol if single char and no name', () => {
    fixture.componentRef.setInput('symbol', 'x');
    fixture.componentRef.setInput('name', '');
    expect(component.initials).toBe('X');
  });

  it('initials should return "??" when symbol and name are empty', () => {
    fixture.componentRef.setInput('symbol', '');
    fixture.componentRef.setInput('name', '');
    expect(component.initials).toBe('??');
  });

  it('should render image if imageUrl is provided', () => {
    fixture.componentRef.setInput('imageUrl', 'https://example.com/btc.png');
    fixture.componentRef.setInput('symbol', 'BTC');
    fixture.detectChanges();

    const img = fixture.nativeElement.querySelector('img');
    expect(img).toBeTruthy();
    expect(img.getAttribute('src')).toBe('https://example.com/btc.png');
  });

  it('should render initials fallback if no imageUrl is provided', () => {
    fixture.componentRef.setInput('imageUrl', null);
    fixture.componentRef.setInput('symbol', 'ETH');
    fixture.detectChanges();

    const fallback = fixture.nativeElement.querySelector('.asset-icon-fallback');
    expect(fallback).toBeTruthy();
    expect(fallback.textContent.trim()).toBe('ET');
  });
});

