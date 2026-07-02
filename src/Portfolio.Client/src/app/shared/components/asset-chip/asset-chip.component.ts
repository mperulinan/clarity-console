import { ChangeDetectionStrategy, Component, input, HostBinding } from '@angular/core';

@Component({
  selector: 'app-asset-chip',
  standalone: true,
  templateUrl: './asset-chip.component.html',
  styleUrl: './asset-chip.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AssetChipComponent {
  readonly symbol = input.required<string>();
  readonly imageUrl = input<string | null | undefined>();
  readonly size = input<'md' | 'sm'>('md');

  @HostBinding('class')
  get hostClasses() {
    return `size-${this.size()}`;
  }
}
