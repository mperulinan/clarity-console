# Project Architecture: Portfolio

This document outlines the architectural design and structural organization of the Portfolio project. 

## Overview
The Portfolio project is a comprehensive portfolio management system designed to track assets, transactions, and performance metrics. It consists of a modern Angular frontend and a robust ASP.NET Core backend following Clean Architecture principles.

---

## Backend Architecture (ASP.NET Core)
The backend is structured using **Clean Architecture** and **Domain-Driven Design (DDD)** principles to ensure separation of concerns, maintainability, and testability.

### Layers
Dependencies flow inward:
└── API
    ├── Application    ── Domain        
    └── Infrastructure ── Domain

1.  **Domain**: 
    - The core of the application. 
    - Contains **Entities** (`Asset`, `Transaction`), **Value Objects**, **Enums**, and core **Interfaces** (`IAssetRepository`, `ITransactionRepository`).
    - Defines domain logic and business rules independent of external frameworks.
2.  **Application**: 
    - Orchestrates the flow of data and application logic.
    - Contains **Application Services** (`PortfolioService`, `AssetSearchService`) and **DTOs** (Data Transfer Objects).
    - Depends only on the Domain layer.
3.  **Infrastructure**: 
    - Implements interfaces defined in the Domain and Application layers.
    - **Persistence**: Uses EF Core with SQL Server (`PortfolioContext`).
    - **External Services**: Integrates with external APIs like CoinGecko and Frankfurter for market data and exchange rates.
4.  **Portfolio.API**: 
    - The entry point for the backend.
    - Contains **Controllers** providing RESTful endpoints, **Background Services** for background tasks (e.g., `AssetCatalogSyncBackgroundService`), and configuration (`Program.cs`, `appsettings.json`).

### Key Technologies
- **Framework**: .NET 10.0
- **ORM**: Entity Framework Core
- **Database**: SQL Server
- **Integration**: HttpClient for external API calls, Memory Cache for performance.

---

## Frontend Architecture (Angular)
The frontend is a modern **Angular** application utilizing standalone components and a feature-based structure.

### Project Structure (`src/app`)
- **features/**: Contains feature-specific modules and components.
    - **dashboard/**: Portfolio overview and metrics visualization.
    - **new-transaction/**: Multi-step wizard for adding transactions.
- **models/**: Defines TypeScript interfaces and classes reflecting the backend DTOs and domain objects.
- **services/**: Contains Angular services for API communication, typically wrapping backend endpoints provided by the API layer.
- **app.config.ts / app.routes.ts**: Standard Angular configuration using the standalone component pattern.

### Key Technologies
- **Framework**: Angular 21
- **Styling**: Tailwind
- **State Management**: Service-based reactive state management.

---

## Workspace Layout
```
Portfolio/
├── src/
│   ├── Portfolio.API/            # Entry point & REST API
│   ├── Portfolio.Application/    # Business Logic & DTOs
│   ├── Portfolio.Client/         # Angular Frontend
│   ├── Portfolio.Domain/         # Core Entities & Interfaces
│   └── Portfolio.Infrastructure/ # Data Persistence & External Integrations
├── tests/
│   ├── Portfolio.API.Tests/
│   ├── Portfolio.Application.Tests/
│   ├── Portfolio.Domain.Tests/
│   └── Portfolio.Infrastructure.Tests/
└── Portfolio.slnx              # Solution file
```
