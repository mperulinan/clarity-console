import { Component, OnInit } from '@angular/core';
import { ApiService } from './services/api.service';

@Component({
    selector: 'app-root',
    templateUrl: './app.component.html',
    styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit {

    constructor(
        private api: ApiService,
    ) { }

    async ngOnInit() {
        let assetsWithHoldings = await this.api.getAssetsWithHoldings();
        console.log("assetsWithHoldings", assetsWithHoldings);
        
    }

    title = 'portfolio';
}
