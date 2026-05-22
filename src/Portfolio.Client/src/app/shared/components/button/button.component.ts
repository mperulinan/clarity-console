import { Component, input, computed, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-button',
  standalone: true,
  templateUrl: './button.component.html',
  styleUrl: './button.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ButtonComponent {
  readonly variant = input<'text' | 'tonal' | 'outlined' | 'filled'>('text');
  readonly color = input<'primary' | 'muted' | 'error'>('primary');
  readonly size = input<'sm' | 'md' | 'lg'>('md');
  readonly justify = input<'start' | 'center' | 'end'>('center');
  readonly disabled = input<boolean>(false);
  readonly type = input<'button' | 'submit' | 'reset'>('button');

  readonly hostClass = computed(
    () =>
      `btn-${this.variant()} color-${this.color()} size-${this.size()} justify-${this.justify()}`
  );
}
