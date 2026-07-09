import { Pipe, PipeTransform, inject, LOCALE_ID } from '@angular/core';

/**
 * Formats a date using the browser's native Intl.DateTimeFormat API.
 * Unlike Angular's built-in DatePipe, this pipe requires NO locale data
 * registration — the browser handles every valid BCP-47 locale natively.
 *
 * Usage:
 *   {{ value | intlDate }}                          → short date  (e.g. 09/07/2025)
 *   {{ value | intlDate : true }}                   → date + time (e.g. 09/07/2025, 18:30)
 *   {{ value | intlDate : 'seconds' }}              → date + time + seconds (e.g. 09/07/2025, 18:30:45)
 *   {{ value | intlDate : false : { dateStyle: 'medium' } }} → custom Intl options
 */
@Pipe({
  name: 'intlDate',
  standalone: true,
  pure: true,
})
export class IntlDatePipe implements PipeTransform {
  private readonly locale = inject(LOCALE_ID);

  transform(
    value: Date | string | number | null | undefined,
    includeTime: boolean | 'seconds' = false,
    options?: Intl.DateTimeFormatOptions
  ): string {
    if (value == null) return '';
    const date = new Date(value as string | number | Date);
    if (isNaN(date.getTime())) return '';

    const resolvedOptions: Intl.DateTimeFormatOptions = options ?? {
      dateStyle: 'short',
      ...(includeTime === true    && { timeStyle: 'short' }),
      ...(includeTime === 'seconds' && { timeStyle: 'medium' }),
    };

    return new Intl.DateTimeFormat(this.locale, resolvedOptions).format(date);
  }
}
