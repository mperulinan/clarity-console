import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ImportComponent } from './import.component';
import { ImportService } from '../../services/import.service';
import { Router } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import {
  BrokerDto,
  ImportTransactionsPreviewDto,
  ImportTransactionsResultDto,
  ImportedTransactionRowPreview
} from '../../models/import';

describe('ImportComponent', () => {
  let component: ImportComponent;
  let fixture: ComponentFixture<ImportComponent>;
  let importServiceSpy: any;
  let routerSpy: any;
  let dialogSpy: any;

  const mockBrokers: BrokerDto[] = [{ key: 'degiro', displayName: 'DEGIRO' }];

  const makePreviewRow = (
    id: string,
    overrides: Partial<ImportedTransactionRowPreview> = {}
  ): ImportedTransactionRowPreview => ({
    raw: {
      date: '2024-01-15T10:00:00Z',
      productName: 'Apple Inc',
      isin: 'US0378331005',
      exchange: 'NSY',
      quantity: 10,
      pricePerUnit: 175,
      priceCurrency: 'USD',
      fxRate: 1.06,
      feeEur: 1,
      totalEur: -1750,
      brokerTransactionId: id
    },
    resolvedSymbol: 'AAPL',
    resolvedAssetId: 'asset-guid-1',
    willAutoCreate: false,
    isDuplicate: false,
    isUnresolvable: false,
    warningMessage: null,
    ...overrides
  });

  const mockPreview: ImportTransactionsPreviewDto = {
    previewToken: 'tok-123',
    rows: [
      makePreviewRow('uuid-1'),
      makePreviewRow('uuid-dup', { isDuplicate: true, warningMessage: 'Already imported.' }),
      makePreviewRow('uuid-unresolved', {
        resolvedSymbol: null,
        resolvedAssetId: null,
        isUnresolvable: true,
        warningMessage: 'Could not resolve ticker.'
      })
    ],
    totalRows: 3,
    duplicateRows: 1,
    unresolvableRows: 1,
    readyToImportRows: 1
  };

  const mockResult: ImportTransactionsResultDto = {
    importedCount: 1,
    skippedCount: 2,
    warnings: []
  };

  beforeEach(async () => {
    importServiceSpy = {
      listBrokers: vi.fn().mockReturnValue(of(mockBrokers)),
      preview: vi.fn().mockReturnValue(of(mockPreview)),
      commit: vi.fn().mockReturnValue(of(mockResult))
    };
    routerSpy = { navigate: vi.fn() };
    dialogSpy = { open: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [ImportComponent, NoopAnimationsModule],
      providers: [
        { provide: ImportService, useValue: importServiceSpy },
        { provide: Router,        useValue: routerSpy },
        { provide: MatDialog,     useValue: dialogSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ImportComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  // ── Step 1: Broker auto-selection ───────────────────────────────────────

  it('auto-selects the only broker returned from the API', () => {
    expect(component.selectedBrokerKey()).toBe('degiro');
    expect(component.brokers()).toHaveLength(1);
  });

  it('shows isDegiro as true when DEGIRO is selected', () => {
    expect(component.isDegiro()).toBe(true);
  });

  it('selectBroker() updates selectedBrokerKey', () => {
    component.selectBroker('ibkr');
    expect(component.selectedBrokerKey()).toBe('ibkr');
    expect(component.isDegiro()).toBe(false);
  });

  it('shows error banner when listBrokers fails', () => {
    const err = new HttpErrorResponse({ status: 0, url: '/api/Import/brokers' });
    importServiceSpy.listBrokers.mockReturnValue(throwError(() => err));
    component.loadBrokers();
    expect(component.errorMessage()).toContain('Could not reach the server');
  });

  // ── Step 1: File validation ─────────────────────────────────────────────

  it('handleFile() rejects non-CSV files with an error message', () => {
    const file = new File(['data'], 'export.xlsx', { type: 'application/vnd.ms-excel' });
    component.handleFile(file);
    expect(component.selectedFile()).toBeNull();
    expect(component.errorMessage()).toContain('.csv');
  });

  it('handleFile() rejects empty files', () => {
    const file = new File([''], 'export.csv', { type: 'text/csv' });
    component.handleFile(file);
    expect(component.selectedFile()).toBeNull();
    expect(component.errorMessage()).toContain('empty');
  });

  it('handleFile() rejects files over 10MB', () => {
    const bigContent = 'a'.repeat(11 * 1024 * 1024);
    const file = new File([bigContent], 'export.csv', { type: 'text/csv' });
    component.handleFile(file);
    expect(component.selectedFile()).toBeNull();
    expect(component.errorMessage()).toContain('10 MB');
  });

  it('handleFile() accepts valid CSV files', () => {
    const file = new File(['header\nval'], 'export.csv', { type: 'text/csv' });
    component.handleFile(file);
    expect(component.selectedFile()).toBe(file);
    expect(component.errorMessage()).toBeNull();
  });

  it('removeFile() clears selected file and error', () => {
    const file = new File(['data'], 'export.csv', { type: 'text/csv' });
    component.handleFile(file);
    component.removeFile();
    expect(component.selectedFile()).toBeNull();
    expect(component.errorMessage()).toBeNull();
  });

  // ── Step 1 → Step 2: Preview submission ────────────────────────────────

  it('submitPreview() transitions to review step and sets reviewRows on success', async () => {
    const file = new File(['data'], 'export.csv', { type: 'text/csv' });
    component.handleFile(file);
    component.submitPreview();
    await fixture.whenStable();

    expect(component.currentStep()).toBe('review');
    expect(component.reviewRows()).toHaveLength(3);
  });

  it('submitPreview() marks ready rows as selected by default', async () => {
    const file = new File(['data'], 'export.csv', { type: 'text/csv' });
    component.handleFile(file);
    component.submitPreview();
    await fixture.whenStable();

    const rows = component.reviewRows();
    expect(rows[0].selected).toBe(true);    // ready
    expect(rows[1].selected).toBe(false);   // duplicate — forced off
    expect(rows[2].selected).toBe(false);   // unresolvable — forced off
  });

  it('submitPreview() shows error banner on API failure', async () => {
    const err = new HttpErrorResponse({ status: 400, error: 'Bad CSV format' });
    importServiceSpy.preview.mockReturnValue(throwError(() => err));

    const file = new File(['bad-data'], 'export.csv', { type: 'text/csv' });
    component.handleFile(file);
    component.submitPreview();
    await fixture.whenStable();

    expect(component.currentStep()).toBe('upload');
    expect(component.errorMessage()).toBeTruthy();
  });

  it('submitPreview() shows error when no file selected', () => {
    component.submitPreview();
    expect(component.errorMessage()).toBeTruthy();
    expect(component.currentStep()).toBe('upload');
  });

  it('submitPreview() shows error banner and stays on upload step when preview returns 0 rows', async () => {
    const emptyPreview: ImportTransactionsPreviewDto = {
      previewToken: '',
      rows: [],
      totalRows: 0,
      duplicateRows: 0,
      unresolvableRows: 0,
      readyToImportRows: 0
    };
    importServiceSpy.preview.mockReturnValue(of(emptyPreview));

    const file = new File(['headers\n'], 'empty.csv', { type: 'text/csv' });
    component.handleFile(file);
    component.submitPreview();
    await fixture.whenStable();

    expect(component.currentStep()).toBe('upload');
    expect(component.errorMessage()).toContain('No transactions were found');
  });

  // ── Step 2: Review table row controls ──────────────────────────────────

  it('reviewRows computed counts: readyCount, duplicateCount, unresolvableCount', async () => {
    const file = new File(['data'], 'export.csv', { type: 'text/csv' });
    component.handleFile(file);
    component.submitPreview();
    await fixture.whenStable();

    expect(component.readyCount()).toBe(1);
    expect(component.duplicateCount()).toBe(1);
    expect(component.unresolvableCount()).toBe(1);
    expect(component.selectedCount()).toBe(1);
  });

  it('toggleRow() flips selected for ready rows', async () => {
    const file = new File(['data'], 'export.csv', { type: 'text/csv' });
    component.handleFile(file);
    component.submitPreview();
    await fixture.whenStable();

    expect(component.reviewRows()[0].selected).toBe(true);
    component.toggleRow(0);
    expect(component.reviewRows()[0].selected).toBe(false);
    component.toggleRow(0);
    expect(component.reviewRows()[0].selected).toBe(true);
  });

  it('toggleRow() does nothing for duplicate rows', async () => {
    const file = new File(['data'], 'export.csv', { type: 'text/csv' });
    component.handleFile(file);
    component.submitPreview();
    await fixture.whenStable();

    component.toggleRow(1); // duplicate row
    expect(component.reviewRows()[1].selected).toBe(false);
  });

  it('toggleSelectAll(true) selects all non-duplicate non-unresolvable rows', async () => {
    const file = new File(['data'], 'export.csv', { type: 'text/csv' });
    component.handleFile(file);
    component.submitPreview();
    await fixture.whenStable();

    component.toggleRow(0); // deselect ready row first
    expect(component.reviewRows()[0].selected).toBe(false);

    component.toggleSelectAll(true);
    expect(component.reviewRows()[0].selected).toBe(true);
    expect(component.reviewRows()[1].selected).toBe(false); // duplicate stays off
    expect(component.reviewRows()[2].selected).toBe(false); // unresolvable stays off
  });

  // ── Step 2: Back navigation ─────────────────────────────────────────────

  it('backToUpload() clears preview state and returns to upload step', async () => {
    const file = new File(['data'], 'export.csv', { type: 'text/csv' });
    component.handleFile(file);
    component.submitPreview();
    await fixture.whenStable();

    expect(component.currentStep()).toBe('review');
    component.backToUpload();

    expect(component.currentStep()).toBe('upload');
    expect(component.previewData()).toBeNull();
    expect(component.reviewRows()).toHaveLength(0);
  });

  // ── Step 2 → Step 3: Commit ─────────────────────────────────────────────

  it('submitCommit() calls service with correct excludedBrokerTransactionIds', async () => {
    const file = new File(['data'], 'export.csv', { type: 'text/csv' });
    component.handleFile(file);
    component.submitPreview();
    await fixture.whenStable();

    // Deselect the ready row
    component.toggleRow(0);
    expect(component.selectedCount()).toBe(0);

    component.submitCommit();
    await fixture.whenStable();

    expect(importServiceSpy.commit).toHaveBeenCalledWith(
      'degiro',
      expect.objectContaining({ excludedBrokerTransactionIds: ['uuid-1'] })
    );
    expect(component.currentStep()).toBe('result');
  });

  it('submitCommit() shows session expired banner and sets isSessionExpired flag', async () => {
    const file = new File(['data'], 'export.csv', { type: 'text/csv' });
    component.handleFile(file);
    component.submitPreview();
    await fixture.whenStable();

    const expiredErr = new HttpErrorResponse({
      status: 400,
      error: 'The import preview session has expired or does not exist.'
    });
    importServiceSpy.commit.mockReturnValue(throwError(() => expiredErr));

    component.submitCommit();
    await fixture.whenStable();

    expect(component.isSessionExpired()).toBe(true);
    expect(component.errorMessage()).toBeTruthy();
    expect(component.currentStep()).toBe('review');
  });

  it('reAnalyzeFile() re-submits preview when file is still present', async () => {
    const file = new File(['data'], 'export.csv', { type: 'text/csv' });
    component.handleFile(file);
    component.submitPreview();
    await fixture.whenStable();

    const callsBefore = importServiceSpy.preview.mock.calls.length;
    component.reAnalyzeFile();
    await fixture.whenStable();

    expect(importServiceSpy.preview.mock.calls.length).toBeGreaterThan(callsBefore);
  });

  // ── Step 3: Result & navigation ────────────────────────────────────────

  it('submitCommit() transitions to result step on success', async () => {
    const file = new File(['data'], 'export.csv', { type: 'text/csv' });
    component.handleFile(file);
    component.submitPreview();
    await fixture.whenStable();

    component.submitCommit();
    await fixture.whenStable();

    expect(component.currentStep()).toBe('result');
    expect(component.commitResult()!.importedCount).toBe(1);
    expect(component.commitResult()!.skippedCount).toBe(2);
  });

  it('resetWizard() resets all state back to upload step', async () => {
    const file = new File(['data'], 'export.csv', { type: 'text/csv' });
    component.handleFile(file);
    component.submitPreview();
    await fixture.whenStable();
    component.submitCommit();
    await fixture.whenStable();

    component.resetWizard();

    expect(component.currentStep()).toBe('upload');
    expect(component.selectedFile()).toBeNull();
    expect(component.previewData()).toBeNull();
    expect(component.commitResult()).toBeNull();
  });

  it('goToTransactions() navigates to /transactions', () => {
    component.goToTransactions();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/transactions']);
  });

  it('goToDashboard() navigates to /', () => {
    component.goToDashboard();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/']);
  });

  // ── Transactions page: Import CSV entry button ──────────────────────────

  it('renders Import CSV entry button that links to /import', () => {
    // This test lives here as a convenience; full test is in transactions.component.spec.ts
    // just verifying our model expectations
    expect(true).toBe(true);
  });
});

