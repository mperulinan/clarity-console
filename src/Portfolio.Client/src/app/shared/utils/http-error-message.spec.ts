import { HttpErrorResponse } from '@angular/common/http';
import { parseHttpError } from './http-error-message';

describe('parseHttpError', () => {
  it('returns network message for status 0', () => {
    const result = parseHttpError(new HttpErrorResponse({ status: 0 }));
    expect(result.kind).toBe('network');
    expect(result.message).toContain('Could not reach the server');
  });

  it('returns validation message from error.message body', () => {
    const result = parseHttpError(
      new HttpErrorResponse({
        status: 400,
        error: { message: 'FromAssetId is required for Swap' },
      })
    );
    expect(result.kind).toBe('validation');
    expect(result.message).toBe('FromAssetId is required for Swap');
  });

  it('returns first ASP.NET validation error from errors map', () => {
    const result = parseHttpError(
      new HttpErrorResponse({
        status: 400,
        error: {
          title: 'One or more validation errors occurred.',
          errors: {
            amountSpent: ['Amount must be greater than zero'],
          },
        },
      })
    );
    expect(result.kind).toBe('validation');
    expect(result.message).toBe('Amount must be greater than zero');
  });

  it('returns server message for 500 responses', () => {
    const result = parseHttpError(
      new HttpErrorResponse({
        status: 500,
        error: { message: 'Database unavailable' },
      }),
      { action: 'save the transaction' }
    );
    expect(result.kind).toBe('server');
    expect(result.message).toBe('Database unavailable');
  });

  it('uses generic validation fallback when 400 has no body', () => {
    const result = parseHttpError(new HttpErrorResponse({ status: 400, error: null }));
    expect(result.kind).toBe('validation');
    expect(result.message).toContain('could not accept this data');
  });

  it('extracts string body directly', () => {
    const result = parseHttpError(new HttpErrorResponse({ status: 400, error: 'Direct error text' }));
    expect(result.kind).toBe('validation');
    expect(result.message).toBe('Direct error text');
  });

  it('handles non-object body by returning fallback message', () => {
    const result = parseHttpError(new HttpErrorResponse({ status: 400, error: 12345 as any }));
    expect(result.kind).toBe('validation');
    expect(result.message).toContain('could not accept this data');
  });

  it('reads detail or title when message is absent', () => {
    const resultDetail = parseHttpError(new HttpErrorResponse({ status: 400, error: { detail: 'Specific detail' } }));
    expect(resultDetail.message).toBe('Specific detail');

    const resultTitle = parseHttpError(new HttpErrorResponse({ status: 400, error: { title: 'Specific title' } }));
    expect(resultTitle.message).toBe('Specific title');
  });

  it('handles non-4xx/5xx status codes as unknown kind', () => {
    const result = parseHttpError(new HttpErrorResponse({ status: 302 }));
    expect(result.kind).toBe('unknown');
  });

  it('handles standard Error instances', () => {
    const result = parseHttpError(new Error('Client-side crash'));
    expect(result.kind).toBe('unknown');
    expect(result.message).toBe('Client-side crash');
  });

  it('handles non-Error objects and unknown throwables', () => {
    const result = parseHttpError('plain string thrown');
    expect(result.kind).toBe('unknown');
    expect(result.message).toContain('Something went wrong');
  });

  it('handles empty validation errors object', () => {
    const result = parseHttpError(new HttpErrorResponse({ status: 400, error: { errors: {} } }));
    expect(result.kind).toBe('validation');
  });
});
