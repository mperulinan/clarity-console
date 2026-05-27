import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { DashboardComponent } from './dashboard.component';
import { PortfolioService } from '../../services/portfolio.service';
import { PortfolioMetrics } from '../../models/portfolio-metrics';

function makeMetrics(overrides: Partial<PortfolioMetrics> = {}): PortfolioMetrics {
    return {
        holdings: [],
        totalPortfolioValueUsd: 0,
        totalCostBasisUsd: 0,
        netDepositsUsd: 0,
        totalUnrealizedProfitLossUsd: 0,
        totalRealizedProfitLossUsd: 0,
        totalProfitLossUsd: 0,
        unrealizedProfitLossPercentage: 0,
        totalReturn: 0,
        ...overrides
    };
}

const mockService = {
    getPortfolioMetrics: vi.fn()
};

async function setup() {
    mockService.getPortfolioMetrics.mockReturnValue(of(makeMetrics()));
    await TestBed.configureTestingModule({
        imports: [DashboardComponent],
        providers: [
            provideRouter([]),
            { provide: PortfolioService, useValue: mockService }
        ]
    }).compileComponents();
}

beforeEach(async () => {
    vi.clearAllMocks();
    await setup();
});

describe('DashboardComponent', () => {
    it('should create', () => {
        const fixture = TestBed.createComponent(DashboardComponent);
        expect(fixture.componentInstance).toBeTruthy();
    });

    it('should call getPortfolioMetrics on init', () => {
        const fixture = TestBed.createComponent(DashboardComponent);
        fixture.detectChanges();
        expect(mockService.getPortfolioMetrics).toHaveBeenCalledTimes(1);
    });

    it('should set isLoading to false after data loads', () => {
        const fixture = TestBed.createComponent(DashboardComponent);
        fixture.detectChanges();
        expect(fixture.componentInstance.isLoading()).toBe(false);
    });

    it('should expose totalValue from loaded metrics', () => {
        mockService.getPortfolioMetrics.mockReturnValue(
            of(makeMetrics({ totalPortfolioValueUsd: 42000 }))
        );
        const fixture = TestBed.createComponent(DashboardComponent);
        fixture.detectChanges();
        expect(fixture.componentInstance.totalValue()).toBe(42000);
    });

    it('should show + sign when profit is positive', () => {
        mockService.getPortfolioMetrics.mockReturnValue(
            of(makeMetrics({ totalProfitLossUsd: 500 }))
        );
        const fixture = TestBed.createComponent(DashboardComponent);
        fixture.detectChanges();
        expect(fixture.componentInstance.isPlPositive()).toBe(true);
        expect(fixture.componentInstance.plSign()).toBe('+');
    });

    it('should show empty sign when profit is negative', () => {
        mockService.getPortfolioMetrics.mockReturnValue(
            of(makeMetrics({ totalProfitLossUsd: -200 }))
        );
        const fixture = TestBed.createComponent(DashboardComponent);
        fixture.detectChanges();
        expect(fixture.componentInstance.isPlPositive()).toBe(false);
        expect(fixture.componentInstance.plSign()).toBe('');
    });

    it('should set isLoading=false even on service error', () => {
        mockService.getPortfolioMetrics.mockReturnValue(
            throwError(() => new Error('network error'))
        );
        const fixture = TestBed.createComponent(DashboardComponent);
        fixture.detectChanges();
        expect(fixture.componentInstance.isLoading()).toBe(false);
    });

    it('getInitials should return first 2 uppercase chars', () => {
        const fixture = TestBed.createComponent(DashboardComponent);
        const comp = fixture.componentInstance;
        expect(comp.getInitials('bitcoin')).toBe('BI');
        expect(comp.getInitials('X')).toBe('X');
        expect(comp.getInitials('')).toBe('??');
    });

    it('openNewTransaction should navigate to /new-transaction', () => {
        const fixture = TestBed.createComponent(DashboardComponent);
        fixture.detectChanges();
        const router = TestBed.inject(Router);
        const navSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);
        fixture.componentInstance.openNewTransaction();
        expect(navSpy).toHaveBeenCalledWith(['/new-transaction']);
    });
});