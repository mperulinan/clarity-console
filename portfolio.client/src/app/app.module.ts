import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { LOCALE_ID, NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

import { CookieService } from 'ngx-cookie-service';
import { MaterialModule } from './material/material.module';
import { NewTransactionComponent } from './new-transaction/new-transaction.component';
import { ReactiveFormsModule } from '@angular/forms';
import { registerLocaleData } from '@angular/common';

import localeEs from '@angular/common/locales/es';
import { TransactionsComponent } from './transactions/transactions.component';
import { HomeComponent } from './home/home.component';
registerLocaleData(localeEs);

@NgModule({
    declarations: [
        AppComponent,
        NewTransactionComponent,
        TransactionsComponent,
        HomeComponent
    ],
    bootstrap: [AppComponent], imports: [BrowserModule,
        AppRoutingModule,
        MaterialModule,
        ReactiveFormsModule], providers: [
            provideAnimationsAsync(),
            CookieService,
            { provide: LOCALE_ID, useValue: 'es' },
            provideHttpClient(withInterceptorsFromDi()),
        ]
})
export class AppModule { }
