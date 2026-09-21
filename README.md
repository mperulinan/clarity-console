# Clarity Console - Financial Asset & Portfolio Management System

A comprehensive portfolio management system designed to track assets, transactions, and performance metrics. Built with a modern Angular frontend and a robust ASP.NET Core backend following Clean Architecture and Domain-Driven Design (DDD) principles.

## Features

- **Dashboard**: Real-time portfolio overview and metrics visualization.
- **Asset Tracking**: Monitor asset allocations, Open P/L, and overall performance.
- **Transaction Management**: Add, update, and categorize transactions (deposits, withdrawals, swaps, rewards) using a user-friendly multi-step wizard.
- **Automated Pricing**: Integrates with external APIs (like CoinGecko and Frankfurter) to fetch real-time market data and exchange rates.

## Architecture

The backend follows **Clean Architecture** and **Domain-Driven Design (DDD)**, built on .NET 10.0. The frontend is a modern **Angular 21** application using standalone components and reactive state management.

> [!NOTE]
> For a deep dive into the project structure, dependency flow, core entities (like `Transaction` and `Inventory`), and layer responsibilities, please read **[`ARCHITECTURE.md`](ARCHITECTURE.md)**.

## Prerequisites

Before setting up the project, ensure you have the following installed:
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) (Recommended LTS version) & npm
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)

## Getting Started

### 1. Clone the Repository
```bash
git clone <repository-url>
cd Portfolio
```

### 2. Backend Setup
Navigate to the API directory and run the application:
```bash
cd src/Portfolio.API
# Ensure your connection strings in appsettings.json are configured correctly targeting your SQL Server instance
dotnet restore
dotnet build
# To run the development server:
dotnet run
```

### 3. Frontend Setup
Navigate to the Angular client application to install dependencies and start the local development server:
```bash
cd ../Portfolio.Client
npm install
npm start
```

The frontend should now be running at `http://localhost:4200` and interacting with the backend API.

## Project Structure

```text
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
└── Portfolio.slnx                # Solution file
```

## Testing

The project contains a comprehensive test suite across multiple layers.

**Backend Tests:**
Located in the `/tests` folder. You can run them via the .NET CLI:
```bash
dotnet test
```

**Frontend Tests:**
Inside the `Portfolio.Client` directory:
```bash
npm run test
```

## License

Please refer to the repository owner or license file for licensing terms and usage rights.
