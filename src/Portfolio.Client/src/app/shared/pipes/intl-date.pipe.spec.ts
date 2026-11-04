import { TestBed } from '@angular/core/testing';
import { IntlDatePipe } from './intl-date.pipe';
import { LOCALE_ID } from '@angular/core';

describe('IntlDatePipe', () => {
  let pipe: IntlDatePipe;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        IntlDatePipe,
        { provide: LOCALE_ID, useValue: 'en-US' }
      ]
    });
    pipe = TestBed.inject(IntlDatePipe);
  });

  it('should be created', () => {
    expect(pipe).toBeTruthy();
  });

  it('should return empty string for null or undefined', () => {
    expect(pipe.transform(null)).toBe('');
    expect(pipe.transform(undefined)).toBe('');
  });

  it('should return empty string for invalid date', () => {
    expect(pipe.transform('invalid-date')).toBe('');
  });

  it('should format date with default short style', () => {
    const formatted = pipe.transform('2024-05-15T12:00:00Z');
    expect(formatted).toBeTruthy();
    expect(formatted.length).toBeGreaterThan(0);
  });

  it('should format date with includeTime = true', () => {
    const formatted = pipe.transform(new Date(2024, 4, 15, 14, 30), true);
    expect(formatted).toContain(':');
  });

  it('should format date with includeTime = "seconds"', () => {
    const formatted = pipe.transform(new Date(2024, 4, 15, 14, 30, 45), 'seconds');
    expect(formatted).toContain(':');
  });

  it('should format date with custom options', () => {
    const formatted = pipe.transform('2024-05-15T12:00:00Z', false, { month: 'long', year: 'numeric' });
    expect(formatted).toContain('May');
    expect(formatted).toContain('2024');
  });
});

