import { Component, input, OnInit, forwardRef, signal, ChangeDetectionStrategy, inject, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR, ReactiveFormsModule, FormControl } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatAutocompleteModule, MatAutocompleteSelectedEvent } from '@angular/material/autocomplete';
import { MatOptionModule } from '@angular/material/core';
import { PortfolioService, AssetDto } from '../../../services/portfolio.service';
import { debounceTime, switchMap, catchError, of, startWith, filter, finalize } from 'rxjs';
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
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => AssetSelectorComponent),
            multi: true
        }
    ],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class AssetSelectorComponent implements ControlValueAccessor, OnInit {
    readonly label = input<string>('Asset');
    readonly placeholder = input<string>('e.g. BTC, ETH');
    readonly chipClass = input<string>('asset-chip');

    searchControl = new FormControl<string | AssetDto | null>('');
    selectedAsset = signal<AssetDto | null>(null);
    filteredAssets = signal<AssetDto[]>([]);
    isSyncingAsset = signal<boolean>(false);

    onChange: any = () => { };
    onTouched: any = () => { };

    private readonly destroyRef = inject(DestroyRef);

    constructor(private portfolioService: PortfolioService) { }

    ngOnInit() {
        this.searchControl.valueChanges.pipe(
            startWith(''),
            filter(value => typeof value === 'string'),
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

        if (!asset.id || asset.id === '00000000-0000-0000-0000-000000000000') {
            this.isSyncingAsset.set(true);
            this.searchControl.disable({ emitEvent: false });

            this.portfolioService.syncAsset(asset).pipe(
                finalize(() => {
                    this.isSyncingAsset.set(false);
                    this.searchControl.enable({ emitEvent: false });
                })
            ).subscribe({
                next: (syncedAsset: AssetDto) => this.setInternalValue(syncedAsset),
                error: (err: any) => {
                    console.error('Failed to sync asset', err);
                    this.clearSelection();
                }
            });
        } else {
            this.setInternalValue(asset);
        }
    }

    onAssetInputBlur() {
        this.onTouched();
        if (typeof this.searchControl.value === 'string') {
            this.clearSelection();
        }
    }

    clearAsset(event?: Event) {
        if (event) {
            event.stopPropagation();
        }
        this.clearSelection();
    }

    private setInternalValue(asset: AssetDto) {
        this.selectedAsset.set(asset);
        this.searchControl.setValue(asset, { emitEvent: false });
        this.onChange(asset);
    }

    private clearSelection() {
        this.selectedAsset.set(null);
        this.searchControl.setValue(null, { emitEvent: false });
        this.onChange(null);
    }

    // ── ControlValueAccessor ─────────────────────────────────────────────
    writeValue(value: AssetDto | null): void {
        this.selectedAsset.set(value);
        this.searchControl.setValue(value, { emitEvent: false });
    }

    registerOnChange(fn: any): void { this.onChange = fn; }
    registerOnTouched(fn: any): void { this.onTouched = fn; }

    setDisabledState(isDisabled: boolean): void {
        isDisabled
            ? this.searchControl.disable({ emitEvent: false })
            : this.searchControl.enable({ emitEvent: false });
    }
}
