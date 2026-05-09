import {
    Component, input, output, OnInit, signal,
    ChangeDetectionStrategy, inject, DestroyRef, effect
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormControl } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatAutocompleteModule, MatAutocompleteSelectedEvent } from '@angular/material/autocomplete';
import { MatOptionModule } from '@angular/material/core';
import { PortfolioService } from '../../../services/portfolio.service';
import { AssetDto } from '../../../models/asset';
import { debounceTime, switchMap, catchError, of, startWith, filter, finalize, map, distinctUntilChanged } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
    selector: 'app-asset-selector',
    standalone: true,
    imports: [
        CommonModule,
        ReactiveFormsModule,
        MatFormFieldModule,
        MatInputModule,
        MatIconModule,
        MatAutocompleteModule,
        MatOptionModule
    ],
    templateUrl: './asset-selector.component.html',
    styleUrl: './asset-selector.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class AssetSelectorComponent implements OnInit {
    // ── Inputs ───────────────────────────────────────────────────────────
    readonly label = input<string>('Asset');
    readonly placeholder = input<string>('e.g. BTC, ETH');
    readonly chipClass = input<string>('asset-chip');
    /** Currently selected asset — drives internal display state. */
    readonly value = input<AssetDto | null>(null);
    /** Whether the selector should be non-interactive. */
    readonly disabled = input<boolean>(false);

    // ── Outputs ──────────────────────────────────────────────────────────
    /** Emits when the user selects or clears an asset. */
    readonly assetChange = output<AssetDto | null>();

    // ── Internal state ───────────────────────────────────────────────────
    searchControl = new FormControl<string | AssetDto | null>('');
    selectedAsset = signal<AssetDto | null>(null);
    filteredAssets = signal<AssetDto[]>([]);
    isSyncingAsset = signal<boolean>(false);
    syncError = signal<string | null>(null);

    private readonly destroyRef = inject(DestroyRef);
    private readonly portfolioService = inject(PortfolioService);

    constructor() {
        // Sync value input → internal display state
        effect(() => {
            const v = this.value();
            this.selectedAsset.set(v);
            this.searchControl.setValue(v, { emitEvent: false });
        });

        // Sync disabled input → search control enabled state
        effect(() => {
            if (this.disabled()) {
                this.searchControl.disable({ emitEvent: false });
            } else if (this.searchControl.disabled && !this.isSyncingAsset()) {
                this.searchControl.enable({ emitEvent: false });
            }
        });
    }

    ngOnInit() {
        this.searchControl.valueChanges.pipe(
            startWith(''),
            filter(value => typeof value === 'string'),
            map(value => value.trim()),
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

    displayAssetFn(asset: AssetDto): string {
        return asset ? `${asset.name} (${asset.symbol})` : '';
    }

    onAssetSelected(event: MatAutocompleteSelectedEvent) {
        const asset = event.option.value as AssetDto;
        if (!asset) return;
        this.syncError.set(null);

        if (!asset.id || asset.id === '00000000-0000-0000-0000-000000000000') {
            this.isSyncingAsset.set(true);
            this.searchControl.disable({ emitEvent: false });

            this.portfolioService.syncAsset(asset).pipe(
                finalize(() => {
                    this.isSyncingAsset.set(false);
                    if (!this.disabled()) {
                        this.searchControl.enable({ emitEvent: false });
                    }
                })
            ).subscribe({
                next: (syncedAsset: AssetDto) => this.setInternalValue(syncedAsset),
                error: () => {
                    this.syncError.set('Could not sync this asset right now. Please try again.');
                    this.selectedAsset.set(null);
                    this.searchControl.setValue(null, { emitEvent: false });
                    this.assetChange.emit(null);
                }
            });
        } else {
            this.setInternalValue(asset);
        }
    }

    onAssetInputBlur() {
        if (typeof this.searchControl.value === 'string') {
            this.clearSelection();
        }
    }

    clearAsset(event?: Event) {
        if (event) event.stopPropagation();
        this.clearSelection();
    }

    private setInternalValue(asset: AssetDto) {
        this.selectedAsset.set(asset);
        this.searchControl.setValue(asset, { emitEvent: false });
        this.assetChange.emit(asset);
    }

    private clearSelection() {
        this.selectedAsset.set(null);
        this.searchControl.setValue(null, { emitEvent: false });
        this.syncError.set(null);
        this.assetChange.emit(null);
    }
}
