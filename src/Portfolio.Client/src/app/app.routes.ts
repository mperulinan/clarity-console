import { Routes } from '@angular/router';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { NewTransactionComponent } from './features/new-transaction/new-transaction.component';

export const routes: Routes = [
    { path: '', component: DashboardComponent },
    { path: 'new-transaction', component: NewTransactionComponent },
    { path: '**', redirectTo: '' }
];
