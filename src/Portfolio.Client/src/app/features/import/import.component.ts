import {
  Component,
  OnInit,
  signal,
  computed,
  inject,
  ChangeDetectionStrategy,
  DestroyRef
} from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import { Router } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog } from '@angular/material/dialog';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { ImportService } from '../../services/import.service';
import {
  BrokerDto,
  ImportTransactionsPreviewDto,
  ImportTransactionsResultDto,
  ImportWizardStep,
  ReviewRowItem
} from '../../models/import';
import { AssetDto } from '../../models/asset';
import { AssetSearchDialogComponent } from '../../shared/components/asset-search-dialog/asset-search-dialog.component';
import { BadgeComponent } from '../../shared/components/badge/badge.component';
import { ButtonComponent } from '../../shared/components/button/button.component';
import { SkeletonComponent } from '../../shared/components/skeleton/skeleton.component';
import { IntlDatePipe } from '../../shared/pipes/intl-date.pipe';
import { parseHttpError } from '../../shared/utils/http-error-message';

@Component({
  selector: 'app-import',
  standalone: true,
  imports: [
    CommonModule,
    DecimalPipe,
    MatIconModule,
    MatTooltipModule,
    BadgeComponent,
    ButtonComponent,
    SkeletonComponent,
    IntlDatePipe
  ],
  templateUrl: './import.component.html',
  styleUrl: './import.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ImportComponent implements OnInit {
  private readonly importService = inject(ImportService);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly currentStep = signal<ImportWizardStep>('upload');

  // Step 1: Brokers & File
  readonly brokers = signal<BrokerDto[]>([]);
  readonly selectedBrokerKey = signal<string>('degiro');
  readonly selectedFile = signal<File | null>(null);
  readonly isDragOver = signal<boolean>(false);
  readonly isLoadingBrokers = signal<boolean>(false);
  readonly isLoadingPreview = signal<boolean>(false);

  // Step 2: Review
  readonly previewData = signal<ImportTransactionsPreviewDto | null>(null);
  readonly reviewRows = signal<ReviewRowItem[]>([]);
  readonly overriddenSymbols = signal<Record<string, string>>({});
  readonly isCommitting = signal<boolean>(false);

  // Step 3: Result
  readonly commitResult = signal<ImportTransactionsResultDto | null>(null);

  // General error banner state
  readonly errorMessage = signal<string | null>(null);
  readonly isSessionExpired = signal<boolean>(false);

  // Computed state
  readonly selectedBroker = computed(() =>
    this.brokers().find(b => b.key === this.selectedBrokerKey())
  );

  readonly isDegiro = computed(() => this.selectedBrokerKey() === 'degiro');

  readonly totalRowCount = computed(() => this.reviewRows().length);

  readonly readyCount = computed(() =>
    this.reviewRows().filter(r => !r.isDuplicate && !r.isUnresolvable).length
  );

  readonly selectedCount = computed(() =>
    this.reviewRows().filter(r => r.selected).length
  );

  readonly duplicateCount = computed(() =>
    this.reviewRows().filter(r => r.isDuplicate).length
  );

  readonly unresolvableCount = computed(() =>
    this.reviewRows().filter(r => r.isUnresolvable).length
  );

  readonly newAssetCount = computed(() =>
    this.reviewRows().filter(r => r.willAutoCreate && !r.isDuplicate && !r.isUnresolvable).length
  );

  readonly allSelectableChecked = computed(() => {
    const selectable = this.reviewRows().filter(r => !r.isDuplicate && !r.isUnresolvable);
    return selectable.length > 0 && selectable.every(r => r.selected);
  });

  readonly someSelectableChecked = computed(() => {
    const selectable = this.reviewRows().filter(r => !r.isDuplicate && !r.isUnresolvable);
    return selectable.some(r => r.selected) && !this.allSelectableChecked();
  });

  ngOnInit(): void {
    this.loadBrokers();
  }

  loadBrokers(): void {
    this.isLoadingBrokers.set(true);
    this.importService
      .listBrokers()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: brokers => {
          this.brokers.set(brokers);
          this.isLoadingBrokers.set(false);
          if (brokers.length > 0) {
            this.selectedBrokerKey.set(brokers[0].key);
          }
        },
        error: err => {
          this.isLoadingBrokers.set(false);
          const parsed = parseHttpError(err, { action: 'fetch available brokers' });
          this.errorMessage.set(parsed.message);
        }
      });
  }

  selectBroker(key: string): void {
    this.selectedBrokerKey.set(key);
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.handleFile(input.files[0]);
    }
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver.set(true);
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver.set(false);
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver.set(false);

    if (event.dataTransfer?.files && event.dataTransfer.files.length > 0) {
      this.handleFile(event.dataTransfer.files[0]);
    }
  }

  handleFile(file: File): void {
    this.errorMessage.set(null);
    this.isSessionExpired.set(false);

    if (!file.name.toLowerCase().endsWith('.csv')) {
      this.errorMessage.set('Please select a valid .csv file.');
      return;
    }

    if (file.size === 0) {
      this.errorMessage.set('The selected CSV file is empty.');
      return;
    }

    const maxBytes = 10 * 1024 * 1024; // 10MB
    if (file.size > maxBytes) {
      this.errorMessage.set('The file size exceeds the maximum allowed limit of 10 MB.');
      return;
    }

    this.selectedFile.set(file);
  }

  removeFile(): void {
    this.selectedFile.set(null);
    this.errorMessage.set(null);
    this.isSessionExpired.set(false);
  }

  submitPreview(): void {
    const file = this.selectedFile();
    const brokerKey = this.selectedBrokerKey();

    if (!file || !brokerKey) {
      this.errorMessage.set('Please choose a broker and upload a CSV file.');
      return;
    }

    this.isLoadingPreview.set(true);
    this.errorMessage.set(null);
    this.isSessionExpired.set(false);

    this.importService
      .preview(brokerKey, file)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: previewDto => {
          this.isLoadingPreview.set(false);

          if (!previewDto.rows || previewDto.rows.length === 0) {
            this.errorMessage.set(
              'No transactions were found in the uploaded CSV file. Please verify that the file contains trade records.'
            );
            return;
          }

          this.previewData.set(previewDto);

          const items: ReviewRowItem[] = previewDto.rows.map(r => ({
            ...r,
            selected: !r.isDuplicate && !r.isUnresolvable
          }));

          this.reviewRows.set(items);
          this.overriddenSymbols.set({});
          this.currentStep.set('review');
        },
        error: err => {
          this.isLoadingPreview.set(false);
          const parsed = parseHttpError(err, { action: 'preview the CSV file' });
          this.errorMessage.set(parsed.message);
        }
      });
  }

  toggleSelectAll(checked: boolean): void {
    this.reviewRows.update(rows =>
      rows.map(row => {
        if (!row.isDuplicate && !row.isUnresolvable) {
          return { ...row, selected: checked };
        }
        return row;
      })
    );
  }

  toggleRow(index: number): void {
    this.reviewRows.update(rows => {
      const copy = [...rows];
      const target = copy[index];
      if (target && !target.isDuplicate && !target.isUnresolvable) {
        copy[index] = { ...target, selected: !target.selected };
      }
      return copy;
    });
  }

  openAssetSearch(index: number): void {
    const targetRow = this.reviewRows()[index];
    if (!targetRow) return;

    const dialogRef = this.dialog.open(AssetSearchDialogComponent, {
      width: '500px',
      data: {}
    });

    dialogRef.afterClosed().subscribe((selectedAsset: AssetDto | undefined) => {
      if (!selectedAsset) return;

      this.reviewRows.update(rows => {
        const copy = [...rows];
        copy[index] = {
          ...copy[index],
          resolvedSymbol: selectedAsset.symbol,
          resolvedAssetId: selectedAsset.id ?? null,
          isUnresolvable: false,
          warningMessage: null,
          selected: true
        };
        return copy;
      });

      this.overriddenSymbols.update(overrides => ({
        ...overrides,
        [targetRow.raw.brokerTransactionId]: selectedAsset.symbol
      }));
    });
  }

  backToUpload(): void {
    // Invalidate flow per UX decision
    this.previewData.set(null);
    this.reviewRows.set([]);
    this.overriddenSymbols.set({});
    this.errorMessage.set(null);
    this.isSessionExpired.set(false);
    this.currentStep.set('upload');
  }

  submitCommit(): void {
    const preview = this.previewData();
    const brokerKey = this.selectedBrokerKey();

    if (!preview || !brokerKey) {
      this.errorMessage.set('Preview session is not initialized. Please re-upload your file.');
      return;
    }

    const excludedIds = this.reviewRows()
      .filter(r => !r.selected && !r.isDuplicate && !r.isUnresolvable)
      .map(r => r.raw.brokerTransactionId);

    const overrides = this.overriddenSymbols();

    this.isCommitting.set(true);
    this.errorMessage.set(null);
    this.isSessionExpired.set(false);

    this.importService
      .commit(brokerKey, {
        previewToken: preview.previewToken,
        excludedBrokerTransactionIds: excludedIds.length > 0 ? excludedIds : undefined,
        overriddenSymbols: Object.keys(overrides).length > 0 ? overrides : undefined
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: resultDto => {
          this.isCommitting.set(false);
          this.commitResult.set(resultDto);
          this.currentStep.set('result');
        },
        error: err => {
          this.isCommitting.set(false);
          const parsed = parseHttpError(err, { action: 'commit transactions' });
          this.errorMessage.set(parsed.message);

          if (
            parsed.message.toLowerCase().includes('expired') ||
            parsed.message.toLowerCase().includes('preview session')
          ) {
            this.isSessionExpired.set(true);
          }
        }
      });
  }

  reAnalyzeFile(): void {
    if (this.selectedFile()) {
      this.submitPreview();
    } else {
      this.backToUpload();
    }
  }

  resetWizard(): void {
    this.selectedFile.set(null);
    this.previewData.set(null);
    this.reviewRows.set([]);
    this.overriddenSymbols.set({});
    this.commitResult.set(null);
    this.errorMessage.set(null);
    this.isSessionExpired.set(false);
    this.currentStep.set('upload');
  }

  goToTransactions(): void {
    this.router.navigate(['/transactions']);
  }

  goToDashboard(): void {
    this.router.navigate(['/']);
  }
}

