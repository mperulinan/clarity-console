import { Component, input, HostBinding } from '@angular/core';

@Component({
  selector: 'app-skeleton',
  standalone: true,
  template: '',
  styleUrl: './skeleton.component.scss'
})
export class SkeletonComponent {
  readonly type = input<'block' | 'circle' | 'bento' | 'line'>('line');
  readonly width = input<string>();
  readonly height = input<string>();

  @HostBinding('class')
  get hostClass() {
    return `skel-${this.type()}`;
  }

  @HostBinding('style.width')
  get hostWidth() {
    return this.width();
  }

  @HostBinding('style.height')
  get hostHeight() {
    return this.height();
  }

  @HostBinding('attr.aria-hidden') ariaHidden = true;
  @HostBinding('attr.aria-busy') ariaBusy = true;
}
