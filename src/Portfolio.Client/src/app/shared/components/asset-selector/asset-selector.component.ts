import {
    Component, input, output, ChangeDetectionStrategy, inject, effect
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { AssetDto } from '../../../models/asset';
import { AssetAvatarComponent } from '../asset-avatar/asset-avatar.component';
import { AssetSearchDialogComponent } from '../asset-search-dialog/asset-search-dialog.component';

@Component({
    selector: 'app-asset-selector',
    standalone: true,
    imports: [
        CommonModule,
        MatFormFieldModule,
        MatInputModule,
        MatIconModule,
        MatDialogModule,
        AssetAvatarComponent
    ],
    templateUrl: './asset-selector.component.html',
    styleUrl: './asset-selector.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class AssetSelectorComponent {
    // ── Inputs ───────────────────────────────────────────────────────────
    readonly placeholder = input<string>('e.g. BTC, ETH');
    readonly chipClass = input<string>('asset-chip');
    /** Currently selected asset — drives internal display state. */
    readonly value = input<AssetDto | null>(null);
    /** Whether the selector should be non-interactive. */
    readonly disabled = input<boolean>(false);

    // ── Outputs ──────────────────────────────────────────────────────────
    /** Emits when the user selects or clears an asset. */
    readonly assetChange = output<AssetDto | null>();

    private readonly dialog = inject(MatDialog);

    openDialog() {
        if (this.disabled()) return;

        const dialogRef = this.dialog.open(AssetSearchDialogComponent, {
            width: '800px',
            maxWidth: '95vw',
            panelClass: 'asset-search-dialog-panel'
        });

        dialogRef.afterClosed().subscribe((result: AssetDto | undefined) => {
            if (result) {
                this.assetChange.emit(result);
            }
        });
    }

    clearAsset(event?: Event) {
        if (event) event.stopPropagation();
        this.assetChange.emit(null);
    }
}
