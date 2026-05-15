import { Component, ChangeDetectionStrategy, inject, OnInit, signal, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { ReactiveFormsModule, FormControl } from '@angular/forms';
import { AssetDto } from '../../../models/asset';
import { PortfolioService } from '../../../services/portfolio.service';
import { debounceTime, switchMap, catchError, of, startWith, filter, finalize, map, distinctUntilChanged } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AssetAvatarComponent } from '../asset-avatar/asset-avatar.component';

@Component({
  selector: 'app-asset-search-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    ReactiveFormsModule,
    AssetAvatarComponent
  ],
  templateUrl: './asset-search-dialog.component.html',
  styleUrl: './asset-search-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AssetSearchDialogComponent implements OnInit {
  searchControl = new FormControl<string>('');
  filteredAssets = signal<AssetDto[]>([]);
  isSyncingAsset = signal<boolean>(false);
  syncError = signal<string | null>(null);

  private readonly destroyRef = inject(DestroyRef);
  private readonly portfolioService = inject(PortfolioService);
  private readonly dialogRef = inject(MatDialogRef<AssetSearchDialogComponent>);

  ngOnInit() {
    this.searchControl.valueChanges.pipe(
      startWith(''),
      filter(value => typeof value === 'string'),
      map(value => value ? value.trim() : ''),
      distinctUntilChanged(),
      debounceTime(300),
      switchMap((value: string) => {
        if (!value || value.length < 2) return of([]);
        return this.portfolioService.searchAssets(value).pipe(
          catchError(() => of([]))
        );
      }),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(assets => this.filteredAssets.set(assets));
  }

  close() {
    this.dialogRef.close();
  }

  selectAsset(asset: AssetDto) {
    if (!asset) return;
    this.syncError.set(null);

    if (!asset.id || asset.id === '00000000-0000-0000-0000-000000000000') {
      this.isSyncingAsset.set(true);
      this.searchControl.disable({ emitEvent: false });

      this.portfolioService.syncAsset(asset).pipe(
        finalize(() => {
          this.isSyncingAsset.set(false);
          this.searchControl.enable({ emitEvent: false });
        })
      ).subscribe({
        next: (syncedAsset: AssetDto) => this.dialogRef.close(syncedAsset),
        error: () => {
          this.syncError.set('Could not sync this asset right now. Please try again.');
        }
      });
    } else {
      this.dialogRef.close(asset);
    }
  }
}
