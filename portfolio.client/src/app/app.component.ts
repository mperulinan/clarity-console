import { Component, OnInit } from '@angular/core';
import { ApiService } from './services/api.service';
import { Coin, CoinGeckoService } from './services/coin-gecko.service';
import { MatDialog } from '@angular/material/dialog';
import { NewTransaction, NewTransactionComponent } from './new-transaction/new-transaction.component';
import { TradeService } from './services/trade.service';

export type PortfolioRow = {
    name: string,
    symbol: string,
    price: number,
    holdingsPrice: number,
    profitLoss: number,
    profitLossPercentage: number,
    holdingsAmount: number,
};

@Component({
    selector: 'app-root',
    templateUrl: './app.component.html',
    styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit {

    displayedColumns: string[] = ['assetId', 'price', 'holdings', 'profitLoss', 'percentage'];
    portfolioRows: PortfolioRow[] = [];
    portfolioValue: number = 0;
    profitLoss: number = 0;
    profitLossPercentage: number = 0;
    currencySymbol: string = "$";

    // Inteface control
    isLoading: boolean = true;

    constructor(
        private api: ApiService,
        private coinGecko: CoinGeckoService,
        private dialog: MatDialog,
        private tradeService: TradeService,
    ) { }

    async ngOnInit() {
        await this.tradeService.loadData();
        await this.coinGecko.loadData(this.tradeService.assets);

        await this.loadTable();
        this.setPortfolioValue();
        this.setProfitLoss();
        this.setProfitLossPercentage();
    }

    async loadTable() {
        const assets: string[] = this.tradeService.assets;

        for (let index = 0; index < assets.length; index++) {
            const asset = assets[index];
            const holdings = this.tradeService.getHoldingsByAsset(asset);

            let coin: Coin | undefined = this.coinGecko.coins.find(c => c.id == asset);

            let price: any = this.coinGecko.prices[asset]?.usd;
            price = price ? price : 0;

            let holdingsPrice: number = holdings * price;
            let profitLoss: number = this.tradeService.getProfitLossInEurosByAsset(asset);
            let newRow: PortfolioRow = {
                name: coin?.name ?? asset,
                symbol: coin?.symbol.toUpperCase() ?? '',
                price: price,
                holdingsPrice: holdingsPrice,
                profitLoss: profitLoss,
                profitLossPercentage: this.tradeService.getProfitLossPercentageByAsset(asset),
                holdingsAmount: holdings,
            };

            this.portfolioRows.push(newRow);
            // if (newRow.holdingsAmount != 0) {
            //     this.portfolioRows.push(newRow);
            // }
        }

        this.portfolioRows.sort((a, b) => b.holdingsPrice - a.holdingsPrice);
        console.log("portfolioRows", this.portfolioRows);


        this.isLoading = false;
    }

    openNewTransactionDialog() {
        let dialogRef = this.dialog.open(NewTransactionComponent, {
            width: '500px',
            data: { coins: this.coinGecko.coins },
        });

        dialogRef.afterClosed().subscribe(async (newTransaction: NewTransaction) => {
            if (!newTransaction) {
                return;
            }

            this.api.postTransaction(newTransaction).then(async result => {
                await this.loadTable();
            }).catch(error => {
                alert(error);
            });
        });
    }

    private setPortfolioValue() {
        let value: number = 0;
        this.portfolioRows.forEach(row => {
            if (row.holdingsPrice > 0) {
                value += row.holdingsPrice;
            }
        });
        this.portfolioValue = value;
    }

    private setProfitLoss() {
        let value: number = 0;
        this.portfolioRows.forEach(row => {
            value += row.profitLoss;
        });
        this.profitLoss = value;
    }

    private setProfitLossPercentage() {
        this.profitLossPercentage = this.profitLoss / this.portfolioValue * 100;
    }

    getAllocation(holdingsPrice: number) {
        let percentage: number = holdingsPrice / this.portfolioValue * 100;
        return percentage >= 0 ? percentage : 0;
    }
}
