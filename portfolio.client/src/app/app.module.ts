import { HttpClientModule } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

import { MatTableModule } from '@angular/material/table';
import { CookieService } from 'ngx-cookie-service';

@NgModule({
    declarations: [
        AppComponent
    ],
    imports: [
        BrowserModule, HttpClientModule,
        AppRoutingModule,
        MatTableModule,
    ],
    providers: [
        provideAnimationsAsync(),
        CookieService,
    ],
    bootstrap: [AppComponent]
})
export class AppModule { }
