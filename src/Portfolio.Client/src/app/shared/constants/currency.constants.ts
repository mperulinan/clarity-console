/**
 * Supported fiat currencies across the application.
 * This is the single source of truth — do NOT hardcode 'USD' or 'EUR' elsewhere.
 */
export type SupportedFiatCurrency = 'USD' | 'EUR';

/**
 * The application-wide default fiat currency.
 * Change this to switch the default for all forms and displays.
 */
export const DEFAULT_FIAT_CURRENCY: SupportedFiatCurrency = 'USD';

/**
 * Maps ISO 4217 currency codes to their localized display symbols.
 */
export const FIAT_CURRENCY_SYMBOLS: Record<SupportedFiatCurrency, string> = {
    'USD': '$',
    'EUR': '€'
};

/**
 * All supported fiat currency codes as a list, useful for dropdowns.
 */
export const SUPPORTED_FIAT_CURRENCIES: SupportedFiatCurrency[] =
    Object.keys(FIAT_CURRENCY_SYMBOLS) as SupportedFiatCurrency[];
