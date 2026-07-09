import { Pipe, PipeTransform, inject, LOCALE_ID } from '@angular/core';

/**
 * Formats a date using the browser's native Intl.DateTimeFormat API.
 * Unlike Angular's built-in DatePipe, this pipe requires NO locale data
 * registration — the browser handles every valid BCP-47 locale natively.
 *
 * Usage:
 *   {{ value | intlDate }}                          → short date (e.g. 15/07/2024)
 *   {{ value | intlDate: { dateStyle: 'medium' } }} → e.g. 15 Jul 2024
 *   {{ value | intlDate: { dateStyle: 'long' } }}   → e.g. 15 July 2024
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
    options: Intl.DateTimeFormatOptions = { dateStyle: 'short' }
  ): string {
    if (value == null) return '';
    const date = new Date(value as string | number | Date);
    if (isNaN(date.getTime())) return '';
    return new Intl.DateTimeFormat(this.locale, options).format(date);
  }
}
