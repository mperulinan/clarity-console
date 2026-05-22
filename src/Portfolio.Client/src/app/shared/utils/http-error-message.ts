import { HttpErrorResponse } from '@angular/common/http';

export type HttpErrorKind = 'validation' | 'network' | 'server' | 'unknown';

export interface ParsedHttpError {
  kind: HttpErrorKind;
  message: string;
}

/** Maps failed HTTP calls to user-facing copy by status and response body. */
export function parseHttpError(
  error: unknown,
  options?: { action?: string }
): ParsedHttpError {
  const action = options?.action ?? 'complete this request';

  if (error instanceof HttpErrorResponse) {
    if (error.status === 0) {
      return {
        kind: 'network',
        message: `Could not reach the server. Check your connection and try again.`,
      };
    }

    const serverMessage = extractServerMessage(error);

    if (error.status >= 400 && error.status < 500) {
      return {
        kind: 'validation',
        message:
          serverMessage ??
          `The server could not accept this data (${error.status}). Review your entries and try again.`,
      };
    }

    if (error.status >= 500) {
      return {
        kind: 'server',
        message:
          serverMessage ??
          `A server error occurred (${error.status}) while trying to ${action}. Please try again later.`,
      };
    }

    return {
      kind: 'unknown',
      message: serverMessage ?? `Something went wrong (${error.status}) while trying to ${action}.`,
    };
  }

  if (error instanceof Error && error.message) {
    return { kind: 'unknown', message: error.message };
  }

  return {
    kind: 'unknown',
    message: `Something went wrong while trying to ${action}. Please try again.`,
  };
}

function extractServerMessage(error: HttpErrorResponse): string | null {
  const body = error.error;

  if (body == null) {
    return typeof error.message === 'string' && !error.message.startsWith('Http failure response')
      ? error.message
      : null;
  }

  if (typeof body === 'string') {
    const trimmed = body.trim();
    return trimmed.length > 0 ? trimmed : null;
  }

  if (typeof body !== 'object') {
    return null;
  }

  const record = body as Record<string, unknown>;

  const fromErrors = firstValidationError(record['errors']);
  if (fromErrors) {
    return fromErrors;
  }

  return (
    readString(record['message']) ??
    readString(record['detail']) ??
    readString(record['title'])
  );
}

function firstValidationError(errors: unknown): string | null {
  if (!errors || typeof errors !== 'object') {
    return null;
  }

  for (const messages of Object.values(errors as Record<string, unknown>)) {
    if (Array.isArray(messages) && messages.length > 0) {
      const first = messages.find(m => typeof m === 'string' && m.length > 0);
      if (typeof first === 'string') {
        return first;
      }
    }
  }

  return null;
}

function readString(value: unknown): string | null {
  if (typeof value !== 'string') {
    return null;
  }
  const trimmed = value.trim();
  return trimmed.length > 0 ? trimmed : null;
}
