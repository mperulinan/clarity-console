import { Routes } from '@angular/router';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { NewTransactionComponent } from './features/new-transaction/new-transaction.component';
import { TransactionsComponent } from './features/transactions/transactions.component';
import { TaxReportComponent } from './features/tax-report/tax-report.component';

export const routes: Routes = [
    { path: '', component: DashboardComponent },
    { path: 'transactions', component: TransactionsComponent },
    { path: 'tax-report', component: TaxReportComponent },
    { path: 'new-transaction', component: NewTransactionComponent },
    { path: '**', redirectTo: '' }
];
