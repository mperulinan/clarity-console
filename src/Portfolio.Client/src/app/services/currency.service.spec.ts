import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { CurrencyService } from './currency.service';
import { environment } from '../../environments/environment';

describe('CurrencyService', () => {
    let service: CurrencyService;
    let httpMock: HttpTestingController;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [
                provideHttpClient(),
                provideHttpClientTesting(),
                CurrencyService
            ]
        });
        service = TestBed.inject(CurrencyService);
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        httpMock.verify();
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    it('should initialize currencies correctly', () => {
        const mockSupported = [{ id: '1', symbol: 'USD', name: 'US Dollar', type: 'fiat', transactionCount: 0 }];
        const mockTax = { id: '2', symbol: 'EUR', name: 'Euro', type: 'fiat', transactionCount: 0 };
        const mockDefault = { id: '1', symbol: 'USD', name: 'US Dollar', type: 'fiat', transactionCount: 0 };

        service.initialize().subscribe();

        const reqSupported = httpMock.expectOne(`${environment.apiUrl}/Asset/fiat-currencies`);
        expect(reqSupported.request.method).toBe('GET');
        reqSupported.flush(mockSupported);

        const reqTax = httpMock.expectOne(`${environment.apiUrl}/Asset/tax-currency`);
        expect(reqTax.request.method).toBe('GET');
        reqTax.flush(mockTax);

        const reqDefault = httpMock.expectOne(`${environment.apiUrl}/Asset/default-currency`);
        expect(reqDefault.request.method).toBe('GET');
        reqDefault.flush(mockDefault);

        expect(service.supportedCurrencies()).toEqual(mockSupported);
        expect(service.taxCurrency()).toEqual(mockTax);
        expect(service.defaultCurrency()).toEqual(mockDefault);
    });
});
