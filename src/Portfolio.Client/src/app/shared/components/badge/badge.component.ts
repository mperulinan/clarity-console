import { Component, input, HostBinding, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-badge',
  standalone: true,
  templateUrl: './badge.component.html',
  styleUrl: './badge.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class BadgeComponent {
  readonly semantic = input<'gain' | 'loss' | 'error' | 'neutral' | 'linked'>('neutral');
  readonly size = input<'sm' | 'md'>('md');

  @HostBinding('class')
  get hostClass() {
    return `badge-${this.semantic()} size-${this.size()}`;
  }
}
