import { Component, OnInit } from '@angular/core';
import { ApiService } from './services/api.service';

@Component({
    selector: 'app-root',
    templateUrl: './app.component.html',
    styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit {

    displayedColumns: string[] = ['assetCode', 'holdings'];
    assetsWithHoldings: any;

    constructor(
        private api: ApiService,
    ) { }

    async ngOnInit() {
        this.assetsWithHoldings = await this.api.getAssetsWithHoldings();
        console.log(this.assetsWithHoldings);
        
    }

    title = 'portfolio';
}
