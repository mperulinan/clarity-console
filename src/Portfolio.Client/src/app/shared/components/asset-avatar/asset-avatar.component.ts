import { Component, input, ChangeDetectionStrategy, HostBinding } from '@angular/core';

@Component({
  selector: 'app-asset-avatar',
  standalone: true,
  templateUrl: './asset-avatar.component.html',
  styleUrl: './asset-avatar.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AssetAvatarComponent {
  readonly imageUrl = input<string | null | undefined>();
  readonly symbol = input<string>('');
  readonly name = input<string>('');
  readonly size = input<number>(36); // default size in px

  @HostBinding('style.--avatar-size')
  get avatarSize() {
    return `${this.size()}px`;
  }

  get initials(): string {
    const sym = this.symbol();
    if (sym && sym.length >= 2) {
      return sym.substring(0, 2).toUpperCase();
    }
    const nm = this.name();
    if (nm && nm.length >= 2) {
      return nm.substring(0, 2).toUpperCase();
    }
    if (sym) {
      return sym.toUpperCase();
    }
    return '??';
  }
}
