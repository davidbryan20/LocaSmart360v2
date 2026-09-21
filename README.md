# LocaSmart360 (LocaWeb360)

An ASP.NET Core MVC web app for online store owners (Locaweb/Tray merchants) to ingest sales data, score every sale for fraud risk, and audit the results.

Sales are dropped into a file-based "data lake" as JSON, run through an ETL pipeline that applies behavioural fraud rules, and stored in PostgreSQL (Supabase). Merchants can then review sales, audit risky transactions, and manage their product catalog.

## Features

- **Authentication**: registration and login with session-based auth, a strong-password policy, and SHA-256 password hashing. Users have a role (`Cargo`): `Lojista` (merchant, default) or `Administrador`.
- **Sales ETL / data lake ingestion** (`/Etl/Integracao`): upload a JSON file or paste JSON. Files land in `wwwroot/DataLake/Raw`, get processed and scored, then move to `wwwroot/DataLake/Processed`. Only administrators can ingest data.
- **Fraud analysis engine**: every sale gets a risk score (0–100) and a written justification. See [Fraud rules](#fraud-rules).
- **Sales audit** (`/Vendas`): review sales, scores and justifications, including geolocation and client IP.
- **Product catalog** (`/Produtos`): create, edit and delete products (SKU, name, category, SEO keywords).
- **ETL run log**: each ETL run is recorded in `EtlLogs` with its status and record count.
- **Offline demo mode**: if the database is unreachable, the login page shows an offline badge and a demo account can still sign in.

## Tech stack

- .NET 10 / ASP.NET Core MVC (Razor views, runtime compilation)
- Entity Framework Core 10 with Npgsql
- PostgreSQL, hosted on Supabase
- Bootstrap (bundled in `wwwroot/lib`)

## Project structure

```
LocaWeb360.slnx
LocaWeb360/
├── Controllers/     Autenticacao, Etl, Home, Produtos, Vendas
├── Data/            LocaSmartDbContext (EF Core)
├── DTOs/            ETL request/response objects
├── Migrations/      EF Core migrations
├── Models/          Usuario, Produto, Venda, EtlLog
├── Repositories/    Venda repository
├── Services/        EtlService, AnaliseFraudeService
├── Utils/           Password hashing helper
├── Views/           Razor views
└── wwwroot/DataLake/  Raw (incoming) and Processed JSON files
```

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A PostgreSQL database (a Supabase project works)
- `dotnet-ef` for migrations: `dotnet tool install --global dotnet-ef`

### Configuration

The app reads its database connection string from the `SupabaseConnection` key and does not ship one. Keep secrets out of source control by using [.NET user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets):

```bash
cd LocaWeb360
dotnet user-secrets set "ConnectionStrings:SupabaseConnection" "Host=<host>;Port=5432;Database=postgres;Username=<user>;Password=<password>;SSL Mode=Require"

# Optional: credentials for the offline demo login
dotnet user-secrets set "ModoDemo:Email" "demo@example.com"
dotnet user-secrets set "ModoDemo:Senha" "<demo-password>"
```

### Run

```bash
cd LocaWeb360
dotnet ef database update   # apply migrations
dotnet run
```

The app starts at `http://localhost:5133` (or `https://localhost:7082` with the `https` profile) and opens on the login page.

## Ingesting sales data

Send a JSON array of sales through the **Integração** screen. Example (see `wwwroot/DataLake/Processed/carga.json` for more):

```json
[
  {
    "ClienteId": "CLI-06",
    "ProdutoId": "b44fcf01-e8e9-4dd5-b6e5-1b694dad912b",
    "ValorTotal": 120.00,
    "DataVenda": "2026-06-04T12:00:00Z",
    "Lat": -23.5505,
    "Lon": -46.6333,
    "IpCliente": "177.185.10.15"
  }
]
```

`ProdutoId` must reference an existing product. Sales are sorted by date before processing so each client's history is evaluated in chronological order, and sales whose `VendaId` already exists are skipped.

## Fraud rules

Each sale is scored from 0 to 100 and labelled **NORMAL** (under 40), **ALERT / suspect** (40–74) or **CRITICAL / blocked** (75+). Rules are additive and the score is capped at 100.

| Rule | Trigger |
| --- | --- |
| Impossible travel | Speed between this and the client's last sale is over 900 km/h (and over 50 km), or simultaneous sales more than 20 km apart |
| IP velocity | Another sale from the same IP address within 10 minutes |
| Card testing | Micro-transaction of R$ 15.00 or less |
| Post-micro-transaction scam | A large purchase right after a micro-transaction (above 2x or 3x the client's average ticket) |
| Cooldown | A value 2x/3x or more above the previous purchase within 24h, or 3x/4x or more after 24h |
| Cold start | First purchase from a new client above R$ 1,000.00 |
| Off-hours | Purchase between 00:00 and 05:59 that doesn't match the client's usual pattern |

The rules live in `Services/EtlService.cs`.
