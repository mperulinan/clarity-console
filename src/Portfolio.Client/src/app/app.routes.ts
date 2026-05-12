import { Routes } from '@angular/router';

export const routes: Routes = [
    {
        path: '',
        title: 'Dashboard • Clarity Console',
        loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent)
    },
    {
        path: 'transactions',
        title: 'Transactions • Clarity Console',
        loadComponent: () => import('./features/transactions/transactions.component').then(m => m.TransactionsComponent)
    },
    {
        path: 'tax-report',
        title: 'Tax Report • Clarity Console',
        loadComponent: () => import('./features/tax-report/tax-report.component').then(m => m.TaxReportComponent)
    },
    {
        path: 'new-transaction',
        title: 'New Transaction • Clarity Console',
        loadComponent: () => import('./features/new-transaction/new-transaction.component').then(m => m.NewTransactionComponent)
    },
    { path: '**', redirectTo: '' }
];
