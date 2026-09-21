# Clarity Console - Financial Asset & Portfolio Management System

A comprehensive, enterprise-grade financial asset and portfolio management system. Built to track multi-currency assets across stocks, ETFs, cryptocurrencies, and fiat, calculate tax-aware profit/loss (P&L), and provide real-time market integrations.

This project serves as a showcase of modern software engineering practices, featuring a **Clean Architecture** backend with **Domain-Driven Design (DDD)** and a highly reactive **Angular 21** frontend utilizing **Signals**.

---

## ✨ Key Features

- **Advanced Dashboard**: Real-time portfolio overview, asset allocation charts, and performance metrics.
- **Tax-Aware Inventory & P&L**: Sophisticated calculation of realized/unrealized profit and loss, handling fees and taxable events accurately.
- **Transaction Wizard**: Streamlined multi-step wizard to manage deposits, withdrawals, swaps, and rewards across fiat, cryptocurrencies, stocks, and ETFs.
- **Real-Time Market Data**: Automated pricing integration with external providers (**Twelve Data** for stocks & ETFs, **CoinGecko** for crypto, and **Frankfurter** for fiat exchange rates).
- **Multi-Currency Support**: Seamless normalization of transactions into a base currency (e.g., EUR/USD) for accurate reporting.

## 🛠️ Technology Stack

### Backend (.NET 10 & C#)
- **Architecture**: Clean Architecture & Domain-Driven Design (DDD)
- **Framework**: ASP.NET Core Web API
- **Data Access**: Entity Framework Core & SQL Server (T-SQL)
- **Testing**: xUnit & NSubstitute (Systematic unit testing of financial logic)

### Frontend (Angular 21 & TypeScript)
- **Framework**: Angular 21 (Standalone Components)
- **State Management**: Angular Signals & RxJS for granular reactivity
- **Styling**: Tailwind CSS & Modern UI elements
- **Formatting**: Dynamic localization and IntlDatePipe for UTC normalization

### Infrastructure & DevOps
- **Containerization**: Docker & Docker Compose
- **Version Control**: Git & GitHub

---

## 🏗️ Architecture Overview

The application strictly adheres to the principles of separation of concerns and dependency inversion. 

> [!NOTE]
> For a deep dive into the project structure, dependency flow, core entities (like `Transaction` and `Inventory`), and layer responsibilities, please read the **[`ARCHITECTURE.md`](ARCHITECTURE.md)** document.

```text
Portfolio/
├── src/
│   ├── Portfolio.API/            # Entry point, Controllers & REST API
│   ├── Portfolio.Application/    # Use Cases, CQRS Handlers & DTOs
│   ├── Portfolio.Client/         # Angular 21 SPA
│   ├── Portfolio.Domain/         # Enterprise Logic, Core Entities & Value Objects
│   └── Portfolio.Infrastructure/ # EF Core DbContext & External API Integrations
├── tests/
│   ├── Portfolio.API.Tests/
│   ├── Portfolio.Application.Tests/
│   ├── Portfolio.Domain.Tests/   # Core financial logic tests (Inventory, P&L)
│   └── Portfolio.Infrastructure.Tests/
```

---

## 🚀 Getting Started

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) (LTS recommended)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (Optional, for running SQL Server easily) or local SQL Server instance.

### Quick Setup

**1. Clone the repository:**
```bash
git clone https://github.com/mperulinan/clarity-console.git
cd Portfolio
```

**2. Backend Setup:**
Configure your SQL Server connection string in `src/Portfolio.API/appsettings.Development.json`.
```bash
cd src/Portfolio.API
dotnet restore
dotnet build
dotnet run
```

**3. Frontend Setup:**
```bash
cd ../Portfolio.Client
npm install
npm start
```
The application will be available at `http://localhost:4200`.

---

## 🧪 Testing

The system includes a robust test suite, particularly focused on validating complex financial calculations in the Domain layer to ensure absolute accuracy in P&L and inventory tracking.

- **Run Backend Tests:** `dotnet test`
- **Run Frontend Tests:** `cd src/Portfolio.Client && npm run test`
