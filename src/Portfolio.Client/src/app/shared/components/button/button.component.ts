import { Component, input, HostBinding } from '@angular/core';

@Component({
  selector: 'app-button',
  standalone: true,
  templateUrl: './button.component.html',
  styleUrl: './button.component.scss'
})
export class ButtonComponent {
  readonly variant = input<'primary' | 'ghost' | 'outline'>('ghost');
  readonly color = input<'primary' | 'muted' | 'error'>('primary');
  readonly disabled = input<boolean>(false);

  @HostBinding('class')
  get hostClass() {
    return `btn-${this.variant()} color-${this.color()}`;
  }

  @HostBinding('attr.disabled')
  get hostDisabled() {
    return this.disabled() ? true : null;
  }
}
