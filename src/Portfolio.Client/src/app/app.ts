import { Component, signal, computed } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';

interface NavItem {
    path: string;
    icon: string;
    label: string;
    group: 'portfolio' | 'tax';
}

@Component({
    selector: 'app-root',
    imports: [RouterOutlet, RouterLink, RouterLinkActive, MatIconModule, MatTooltipModule],
    templateUrl: './app.html',
    styleUrl: './app.scss'
})
export class App {
    protected readonly title = signal('Portfolio.Client');

    sidebarExpanded = signal(false);

    readonly navItems: NavItem[] = [
        { path: '/', icon: 'dashboard', label: 'Dashboard', group: 'portfolio' },
        { path: '/transactions', icon: 'receipt_long', label: 'Transactions', group: 'portfolio' },
        { path: '/tax-report', icon: 'assessment', label: 'Tax Report', group: 'tax' },
    ];

    portfolioItems = computed(() => this.navItems.filter(i => i.group === 'portfolio'));
    taxItems = computed(() => this.navItems.filter(i => i.group === 'tax'));

    toggleSidebar() {
        this.sidebarExpanded.update(v => !v);
    }
}
