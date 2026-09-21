# Architecture & System Design: Clarity Console

This document details the architectural principles, domain concepts, and structural patterns used in **Clarity Console**.

---

## 🎯 Overview

Clarity Console is an enterprise-grade financial asset and portfolio management system designed to track multi-currency assets across **stocks, ETFs, cryptocurrencies, and fiat currencies**. 

It is engineered following **Clean Architecture** and **Domain-Driven Design (DDD)** in the backend (.NET 10), coupled with a modern, highly reactive **Angular 21** frontend powered by **Angular Signals**.

---

## 🏗️ Backend Architecture (.NET 10)

The backend follows the principles of Clean Architecture and DDD, enforcing strict separation of concerns where dependencies flow inward toward the Domain core.

### Layer Dependency Diagram

```text
               ┌───────────────────────┐
               │     Portfolio.API     │
               └───────────┬───────────┘
                           │
             ┌─────────────┴─────────────┐
             ▼                           ▼
┌─────────────────────────┐ ┌───────────────────────────┐
│  Portfolio.Application  │ │ Portfolio.Infrastructure  │
└────────────┬────────────┘ └─────────────┬─────────────┘
             │                            │
             └─────────────┬──────────────┘
                           ▼
             ┌───────────────────────────┐
             │     Portfolio.Domain      │
             └───────────────────────────┘
```

### Layer Responsibilities

1. **Domain Layer (`Portfolio.Domain`)**:
   - The heart of the application containing core business rules, enterprise logic, and domain contracts.
   - **Entities & Value Objects**: `Asset`, `Transaction`, `Holdings`, `AssetMarketData`, `TransactionType`.
   - **Domain Services**: `InventoryCalculator` (FIFO/Cost-Basis P&L calculation), `PortfolioMetricsCalculator`, `AssetMarketDataService`.
   - **Abstractions**: Core interfaces like `IUnitOfWork`, `IAssetRepository`, `ITransactionRepository`, `IExchangeRateProvider`.
   - Has zero external dependencies.

2. **Application Layer (`Portfolio.Application`)**:
   - Orchestrates use cases, command/query execution, and DTO mappings.
   - **Handlers & Services**: `SyncAssetCommandHandler`, `PortfolioService`, `AssetSearchService`.
   - **DTOs**: Application-specific data structures for request/response payloads.
   - Depends exclusively on the Domain layer.

3. **Infrastructure Layer (`Portfolio.Infrastructure`)**:
   - Handles data persistence, framework integration, and external service communication.
   - **Persistence**: Entity Framework Core with SQL Server (`PortfolioContext`). Implements the Unit of Work and Repository patterns.
   - **External Market Data Integration**:
     - **Twelve Data Provider**: Real-time stock tickers, market data, and ETFs.
     - **CoinGecko Provider**: Cryptocurrency pricing, search, and historical market data.
     - **Frankfurter Provider**: Foreign exchange (FX) rates for fiat currency normalization.
   - **Background Jobs**: Scheduled sync and background data maintenance services.

4. **API Layer (`Portfolio.API`)**:
   - The HTTP entry point exposing RESTful endpoints.
   - **Controllers**: Thin controllers acting purely as HTTP adapters delegating work to Application handlers.
   - **Configuration & Middleware**: `Program.cs`, CORS, logging, and dependency injection setup.

---

## 🧠 Core Domain Concepts

### 1. Inventory & Tax-Aware P&L Calculation (`InventoryCalculator`)
The system dynamically processes historical transactions (deposits, withdrawals, swaps, rewards) to compute real-time holdings and Profit/Loss (P&L):
- **Taxable Events**: Swaps and asset disposals trigger tax-aware realized P&L tracking.
- **Non-Taxable Events**: Deposits and withdrawals update asset inventory and cost bases without generating premature taxable events.
- **Fee Handling**: Fees incurred during transactions are factored into cost bases or realized P&L depending on the transaction type.

### 2. Multi-Currency Normalization
All asset prices and transaction values are dynamically normalized to a user-defined base currency (e.g., EUR/USD) using real-time and historical exchange rates from external providers.

---

## 🎨 Frontend Architecture (Angular 21)

The frontend is a standalone-component Angular 21 application built for performance and granular reactivity.

### Key Architectural Highlights
- **State Management**: Uses **Angular Signals** for Fine-Grained Reactivity and state updates, alongside **RxJS** for asynchronous event streams (`forkJoin`, `switchMap`).
- **Date & Currency Normalization**: Custom `IntlDatePipe` ensuring dynamic locale formatting and UTC date synchronization across timezones.
- **UX Components**: Multi-step transaction wizard, dynamic spot price calculator, and interactive portfolio dashboard.

---

## 🛠️ Design Patterns & Best Practices

- **Repository & Unit of Work Patterns**: All database operations are staged via repositories and committed atomically via `IUnitOfWork.SaveChangesAsync()`, guaranteeing ACID compliance across complex domain operations.
- **Thin Controllers**: API endpoints do not contain business logic; they delegate execution directly to Application services.
- **Systematic Unit Testing**: Complex domain rules (P&L calculations, tax rules, fee adjustments) are exhaustively tested using **xUnit** and **NSubstitute** to ensure zero regressions in financial logic.
