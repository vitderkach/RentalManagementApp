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
2. Update `appsettings.json` `ConnectionStrings:DefaultConnection` if needed.
3. Run the app - migrations are applied and the database is seeded automatically on startup:
   ```bash
   cd RentalManagementApp
   dotnet run
   ```

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

## Key business rules

- A unit is unavailable while any lease's term covers today.
- Approving a submitted application issues a twelve-month lease and is blocked if the unit
  already has an active lease.
- Applications can only be edited while in `Draft` or `Returned` status; `Approved`, `Denied`,
  and `Withdrawn` are terminal.
- An inactive unit type remains valid on units that already use it but cannot be assigned to any
  other unit (enforced server-side).
- Returning or denying an application requires a comment; approving does not.
