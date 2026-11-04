import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { PortfolioService } from './portfolio.service';
import { environment } from '../../environments/environment';
import { PortfolioReport } from '../models/portfolio-report';
import { AssetDto } from '../models/asset';

describe('PortfolioService', () => {
  let service: PortfolioService;
  let httpMock: HttpTestingController;

  const mockAsset: AssetDto = {
    id: 'asset-1',
    symbol: 'BTC',
    name: 'Bitcoin',
    type: 'Crypto',
    transactionCount: 0
  };

  const mockReport: PortfolioReport = {
    reportingCurrency: 'USD',
    transactions: [],
    holdings: [],
    yearSummaries: [
      { year: 2024, totalGains: 5000, totalLosses: -1000, netPL: 4000, disallowedLosses: 0, eventCount: 3, errorCount: 0 }
    ]
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        PortfolioService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(PortfolioService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify(); // Ensures no unexpected requests are outstanding
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('getPortfolioMetrics() should call the correct endpoint', () => {
    service.getPortfolioMetrics().subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/Portfolio/metrics`);
    expect(req.request.method).toBe('GET');
    req.flush({});
  });

  it('getPortfolioReport() should call the correct endpoint and normalize numeric fields', () => {
    const rawReport = {
      ...mockReport,
      yearSummaries: [
        {
          year: '2024', totalGains: '5000', totalLosses: '-1000',
          netPL: '4000', disallowedLosses: '0'
        }
      ],
      transactions: [{
        profitLoss: '50',
        totalLossAmount: '0',
        disallowsPreviousLosses: [],
        isLossDisallowed: false,
        transaction: {
          id: 1,
          type: 'BUY',
          date: '2024-01-01T00:00:00Z',
          amountSpent: '100',
          amountReceived: '1',
          fee: '5'
        }
      }]
    };

    let result: PortfolioReport | undefined;
    service.getPortfolioReport().subscribe(r => result = r);

    const reqTypes = httpMock.expectOne(`${environment.apiUrl}/Transaction/types`);
    expect(reqTypes.request.method).toBe('GET');
    reqTypes.flush([{ value: 'BUY', name: 'Buy', requiresFromAsset: false, requiresToAsset: true, requiresSpotPrice: true }]);

    const req = httpMock.expectOne(`${environment.apiUrl}/Portfolio/tax-report`);
    expect(req.request.method).toBe('GET');
    req.flush(rawReport);

    expect(result?.yearSummaries[0].year).toBe(2024);
    expect(typeof result?.yearSummaries[0].totalGains).toBe('number');
    expect(result?.transactions[0].transaction.amountSpent).toBe(100);
  });

  it('getTransactionTypes() should call the correct endpoint', () => {
    service.getTransactionTypes().subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/Transaction/types`);
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('addTransaction() should POST to the correct endpoint', () => {
    const mockRequest: any = {
      date: '2024-01-01T00:00:00Z',
      transactionTypeCode: 'BUY',
      toAssetId: 'asset-1',
      amountReceived: 1,
      amountSpent: 50000,
      fee: 0
    };
    service.addTransaction(mockRequest).subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/Transaction`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(mockRequest);
    req.flush(null);
  });

  it('searchAssets() should call the correct endpoint with query params', () => {
    service.searchAssets('BTC').subscribe();

    const req = httpMock.expectOne(r =>
      r.url === `${environment.apiUrl}/Asset/search` && r.params.get('query') === 'BTC'
    );
    expect(req.request.method).toBe('GET');
    req.flush([mockAsset]);
  });

  it('searchAssets() should include optional type param when provided', () => {
    service.searchAssets('BTC', 'Crypto').subscribe();

    const req = httpMock.expectOne(r =>
      r.url === `${environment.apiUrl}/Asset/search` &&
      r.params.get('query') === 'BTC' &&
      r.params.get('type') === 'Crypto'
    );
    expect(req.request.method).toBe('GET');
    req.flush([mockAsset]);
  });

  it('syncAsset() should POST to the correct endpoint', () => {
    service.syncAsset(mockAsset).subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/Asset/sync`);
    expect(req.request.method).toBe('POST');
    req.flush(mockAsset);
  });

  it('getFiatCurrencies() should call the correct endpoint', () => {
    service.getFiatCurrencies().subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/Asset/fiat-currencies`);
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('updateTransaction() should PUT to the correct endpoint', () => {
    const mockRequest: any = { fee: 10 };
    service.updateTransaction(1, mockRequest).subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/Transaction/1`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(mockRequest);
    req.flush(null);
  });

  it('deleteTransaction() should DELETE to the correct endpoint', () => {
    service.deleteTransaction(1).subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/Transaction/1`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });

  it('getTransaction() should call endpoint and normalize numeric fields', () => {
    let result: any;
    service.getTransaction(1).subscribe(tx => result = tx);

    const reqTypes = httpMock.expectOne(`${environment.apiUrl}/Transaction/types`);
    reqTypes.flush([{ value: 'BUY', name: 'Buy', requiresFromAsset: false, requiresToAsset: true, requiresSpotPrice: true }]);

    const req = httpMock.expectOne(`${environment.apiUrl}/Transaction/1`);
    req.flush({
      id: 1,
      type: 'BUY',
      date: '2024-01-01T00:00:00Z',
      amountSpent: '100',
      amountReceived: '1',
      fee: '5'
    });

    expect(result.id).toBe(1);
    expect(result.amountSpent).toBe(100);
    expect(result.amountReceived).toBe(1);
  });

  it('getProcessedTransaction() should call endpoint and normalize numeric fields', () => {
    let result: any;
    service.getProcessedTransaction(1).subscribe(pt => result = pt);

    const reqTypes = httpMock.expectOne(`${environment.apiUrl}/Transaction/types`);
    reqTypes.flush([{ value: 'BUY', name: 'Buy', requiresFromAsset: false, requiresToAsset: true, requiresSpotPrice: true }]);

    const req = httpMock.expectOne(`${environment.apiUrl}/Transaction/1/processed`);
    req.flush({
      profitLoss: '50',
      totalLossAmount: '0',
      disallowsPreviousLosses: [],
      isLossDisallowed: false,
      transaction: {
        id: 1,
        type: 'BUY',
        date: '2024-01-01T00:00:00Z',
        amountSpent: '100',
        amountReceived: '1',
        fee: '5'
      }
    });

    expect(result.profitLoss).toBe(50);
  });

  it('getProcessedTransactions() should call endpoint and normalize transactions', () => {
    let result: any;
    service.getProcessedTransactions('USD').subscribe(res => result = res);

    const reqTypes = httpMock.expectOne(`${environment.apiUrl}/Transaction/types`);
    reqTypes.flush([{ value: 'BUY', name: 'Buy', requiresFromAsset: false, requiresToAsset: true, requiresSpotPrice: true }]);

    const req = httpMock.expectOne(r =>
      r.url === `${environment.apiUrl}/Transaction/processed` && r.params.get('currency') === 'USD'
    );
    expect(req.request.method).toBe('GET');
    req.flush([{
      profitLoss: '50',
      totalLossAmount: '0',
      disallowsPreviousLosses: [],
      isLossDisallowed: false,
      transaction: {
        id: 1,
        type: 'BUY',
        date: '2024-01-01T00:00:00Z',
        amountSpent: '100',
        amountReceived: '1',
        fee: '5'
      }
    }]);

    expect(result.length).toBe(1);
    expect(result[0].profitLoss).toBe(50);
    expect(result[0].transaction.amountSpent).toBe(100);
  });

  it('getSpotPrice() should call endpoint with fiatCurrency and optional date', () => {
    const testDate = new Date('2024-01-01T00:00:00Z');
    service.getSpotPrice('asset-1', 'USD', testDate).subscribe();

    const req = httpMock.expectOne(r =>
      r.url === `${environment.apiUrl}/Asset/asset-1/price` &&
      r.params.get('fiatCurrency') === 'USD' &&
      r.params.get('date') === testDate.toISOString()
    );
    expect(req.request.method).toBe('GET');
    req.flush(50000);
  });

  it('getTaxCurrency() should call the correct endpoint', () => {
    service.getTaxCurrency().subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/Asset/tax-currency`);
    expect(req.request.method).toBe('GET');
    req.flush(mockAsset);
  });
});
