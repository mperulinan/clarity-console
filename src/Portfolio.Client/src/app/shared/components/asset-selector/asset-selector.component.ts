import { Component, Input, OnInit, forwardRef, signal, WritableSignal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR, ReactiveFormsModule, FormControl } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatAutocompleteModule, MatAutocompleteSelectedEvent } from '@angular/material/autocomplete';
import { MatOptionModule } from '@angular/material/core';
import { PortfolioService, AssetDto } from '../../../services/portfolio.service';
import { debounceTime, switchMap, catchError, of, startWith, filter, finalize } from 'rxjs';

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
    ]
})
export class AssetSelectorComponent implements ControlValueAccessor, OnInit {
    @Input() label: string = 'Asset';
    @Input() placeholder: string = 'e.g. BTC, ETH';
    @Input() chipClass: string = 'asset-chip';

    searchControl = new FormControl<string | AssetDto | null>('');
    selectedAsset = signal<AssetDto | null>(null);
    filteredAssets = signal<AssetDto[]>([]);
    isSyncingAsset = signal<boolean>(false);

    onChange: any = () => {};
    onTouched: any = () => {};

    constructor(private portfolioService: PortfolioService) {}

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
            })
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
                next: (syncedAsset: AssetDto) => {
                    this.setInternalValue(syncedAsset);
                },
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
        
        // If string remains, it means they didn't pick an autocomplete option
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

    // ControlValueAccessor methods
    writeValue(value: AssetDto | null): void {
        this.selectedAsset.set(value);
        this.searchControl.setValue(value, { emitEvent: false });
    }

    registerOnChange(fn: any): void {
        this.onChange = fn;
    }

    registerOnTouched(fn: any): void {
        this.onTouched = fn;
    }

    setDisabledState?(isDisabled: boolean): void {
        if (isDisabled) {
            this.searchControl.disable({ emitEvent: false });
        } else {
            this.searchControl.enable({ emitEvent: false });
        }
    }
}
