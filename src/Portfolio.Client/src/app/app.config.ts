import { ApplicationConfig, provideBrowserGlobalErrorListeners, provideAppInitializer, inject, LOCALE_ID } from '@angular/core';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { provideHttpClient, withFetch } from '@angular/common/http';
import { provideNativeDateAdapter } from '@angular/material/core';
import { CurrencyService } from './services/currency.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withFetch()),
    provideNativeDateAdapter(),
    provideAppInitializer(() => {
      const currencyService = inject(CurrencyService);
      return currencyService.initialize();
    }),
    // Used by the Material datepicker (MAT_DATE_LOCALE).
    // IntlDatePipe handles date formatting via the browser's native Intl API,
    // so no registerLocaleData() call is needed.
    { provide: LOCALE_ID, useFactory: () => navigator.language },
  ]
};