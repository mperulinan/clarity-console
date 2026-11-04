import { mapToDashboardRows } from './dashboard.mapper';
import { PortfolioMetrics } from '../../models/portfolio-metrics';

describe('dashboard.mapper', () => {
  it('should return empty array if metrics or holdings are missing', () => {
    expect(mapToDashboardRows(null as any)).toEqual([]);
    expect(mapToDashboardRows({} as any)).toEqual([]);
  });

  it('should map holdings and sort by holdingsPrice descending', () => {
    const metrics: PortfolioMetrics = {
      totalPortfolioValueUsd: 1000,
      totalCostBasisUsd: 800,
      netDepositsUsd: 800,
      totalUnrealizedProfitLossUsd: 200,
      totalRealizedProfitLossUsd: 50,
      totalProfitLossUsd: 250,
      unrealizedProfitLossPercentage: 25,
      totalReturn: 25,
      holdings: [
        {
          id: 'eth-1',
          name: '',
          symbol: '',
          imageUrl: 'eth.png',
          currentPrice: 2000,
          currentValue: 100,
          quantity: 0.05,
          avgCost: 1800,
          openPL: 10,
          realizedPL: 0,
          totalPL: 10,
          openReturn: 5,
          allocationPercentage: 10,
          totalCostBasis: 90,
          costBasisOfSold: 0,
          realizedReturn: 0,
          totalReturn: 0
        },
        {
          id: 'btc-1',
          name: 'Bitcoin',
          symbol: 'btc',
          imageUrl: 'btc.png',
          currentPrice: 50000,
          currentValue: 500,
          quantity: 0.01,
          avgCost: 40000,
          openPL: 100,
          realizedPL: 0,
          totalPL: 100,
          openReturn: 25,
          allocationPercentage: 50,
          totalCostBasis: 400,
          costBasisOfSold: 0,
          realizedReturn: 0,
          totalReturn: 0
        }
      ]
    };

    const rows = mapToDashboardRows(metrics);
    expect(rows.length).toBe(2);
    expect(rows[0].symbol).toBe('BTC');
    expect(rows[0].name).toBe('Bitcoin');
    expect(rows[0].holdingsPrice).toBe(500);
    expect(rows[1].symbol).toBe('ETH-1');
    expect(rows[1].name).toBe('eth-1');
    expect(rows[1].holdingsPrice).toBe(100);
  });
});
