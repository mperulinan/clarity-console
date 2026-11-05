import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { ImportService } from './import.service';
import { environment } from '../../environments/environment';
import {
  BrokerDto,
  CommitImportTransactionsRequest,
  ImportTransactionsPreviewDto,
  ImportTransactionsResultDto
} from '../models/import';

describe('ImportService', () => {
  let service: ImportService;
  let httpMock: HttpTestingController;
  const apiUrl = environment.apiUrl;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        ImportService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(ImportService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('listBrokers', () => {
    it('should issue a GET request to /Import/brokers and return brokers list', () => {
      const mockBrokers: BrokerDto[] = [
        { key: 'degiro', displayName: 'DEGIRO' }
      ];

      service.listBrokers().subscribe(brokers => {
        expect(brokers).toEqual(mockBrokers);
      });

      const req = httpMock.expectOne(`${apiUrl}/Import/brokers`);
      expect(req.request.method).toBe('GET');
      req.flush(mockBrokers);
    });
  });

  describe('preview', () => {
    it('should post multipart form-data to /Import/{brokerKey}/preview', () => {
      const mockFile = new File(['header1,header2\nval1,val2'], 'degiro.csv', { type: 'text/csv' });
      const mockPreviewResponse: ImportTransactionsPreviewDto = {
        previewToken: 'tok-123',
        rows: [],
        totalRows: 0,
        duplicateRows: 0,
        unresolvableRows: 0,
        readyToImportRows: 0
      };

      service.preview('degiro', mockFile).subscribe(res => {
        expect(res).toEqual(mockPreviewResponse);
      });

      const req = httpMock.expectOne(`${apiUrl}/Import/degiro/preview`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body instanceof FormData).toBe(true);
      const fileInForm = req.request.body.get('file');
      expect(fileInForm).toBeTruthy();
      req.flush(mockPreviewResponse);
    });
  });

  describe('commit', () => {
    it('should post json request to /Import/{brokerKey}/commit', () => {
      const request: CommitImportTransactionsRequest = {
        previewToken: 'tok-123',
        excludedBrokerTransactionIds: ['uuid-99']
      };

      const mockResult: ImportTransactionsResultDto = {
        importedCount: 5,
        skippedCount: 1,
        warnings: []
      };

      service.commit('degiro', request).subscribe(res => {
        expect(res).toEqual(mockResult);
      });

      const req = httpMock.expectOne(`${apiUrl}/Import/degiro/commit`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(request);
      req.flush(mockResult);
    });
  });
});

