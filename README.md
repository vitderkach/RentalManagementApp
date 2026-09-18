# Property Rental Management System

A full-stack ASP.NET Core MVC (.NET 10) application for a property management company that
accepts rental applications for its apartments.

## Stack

- ASP.NET Core MVC + Razor views, partial views, and view components
- ASP.NET Core Identity (roles: `Applicant`, `PropertyManager`)
- Entity Framework Core (SQL Server), code-first migrations
- [Bogus](https://github.com/bchavez/Bogus) for idempotent startup seeding
- xUnit for business-logic unit tests

## Running locally

1. Start a SQL Server (or SQL Server Edge/Express) instance, e.g. via Docker:
   ```bash
   docker run -e "ACCEPT_EULA=1" -e "MSSQL_SA_PASSWORD=YourStrong!Passw0rd" \
     -p 1433:1433 --name rentalsql -d mcr.microsoft.com/mssql/server:2022-latest
   # On Apple Silicon, use mcr.microsoft.com/azure-sql-edge instead.
   ```
2. Configure `RentalManagementApp/appsettings.Development.json` (or the
   `ConnectionStrings__DefaultConnection` environment variable) with the SQL Server password
   you chose. The database name may be left as `RentalManagementApp`.
3. Run the app. Migrations are applied and the database is seeded automatically on startup:
   ```bash
   cd RentalManagementApp
   dotnet run
   ```

### Docker Compose

The included Compose setup starts both the web application and SQL Server:

```bash
docker compose up --build
```

Before running it, replace the placeholder password in
`docker-compose.yml`'s `ConnectionStrings__DefaultConnection` with the value of
`MSSQL_SA_PASSWORD`. The app is then available at `http://localhost:8080`.

Seeded accounts (password `Passw0rd!2024` for all):
- Property managers: `manager1@rentalapp.test` .. `manager3@rentalapp.test`
- Applicants: `applicant1@rentalapp.test` .. `applicant10@rentalapp.test`

## Tests

```bash
dotnet test
```

## Project layout

- `Data/Entities` - EF Core entities (Property, Unit, UnitType, RentalApplication, Residence, Lease, ApplicationStatusHistory)
- `Data/Configurations` - fluent entity configuration
- `Data/Seed` - idempotent Bogus-based seeder
- `Services` - business logic (application workflow, lease/availability rules, property/unit management), covered by `RentalManagementApp.Tests`
- `ViewModels` - view models per feature area
- `ViewComponents` - reusable wizard sections and status badge
- `Controllers` / `Views` - MVC controllers and Razor views, including modal-backed partial views
