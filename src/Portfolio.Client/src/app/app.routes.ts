import { Routes } from '@angular/router';

export const routes: Routes = [
    {
        path: '',
        loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent)
    },
    {
        path: 'transactions',
        loadComponent: () => import('./features/transactions/transactions.component').then(m => m.TransactionsComponent)
    },
    {
        path: 'tax-report',
        loadComponent: () => import('./features/tax-report/tax-report.component').then(m => m.TaxReportComponent)
    },
    {
        path: 'new-transaction',
        loadComponent: () => import('./features/new-transaction/new-transaction.component').then(m => m.NewTransactionComponent)
    },
    { path: '**', redirectTo: '' }
];
