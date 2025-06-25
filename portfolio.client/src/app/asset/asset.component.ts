import { Component, OnInit } from '@angular/core';
import { TradeService } from '../services/trade.service';
import { Trade } from '../models/trade';
import { ActivatedRoute } from '@angular/router';

@Component({
    selector: 'app-asset',
    templateUrl: './asset.component.html',
    styleUrl: './asset.component.scss'
})
export class AssetComponent implements OnInit {
    transactions: Trade[] = [];
    assetName: string = '';

    isLoading: boolean = true;
    transactionColumns: string[] = ['id', 'date', 'transactionType', 'from', 'to', 'fromAssetPriceInEur', 'fee', 'feeAssetPriceInEur'];

    constructor(
        private tradeService: TradeService,
        private route: ActivatedRoute
    ) { }

    ngOnInit(): void {
        this.route.paramMap.subscribe(async params => {
            this.assetName = params.get('assetName') || '';

            await this.tradeService.loadData();
            this.transactions = this.tradeService.getTradesByAsset(this.assetName);
            this.transactions.sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime());
            this.isLoading = false;
        });
    }
}
