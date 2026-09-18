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

1. Start a local SQL Server instance.
2. Configure the `ConnectionStrings:DefaultConnection` value in
   `RentalManagementApp/appsettings.Development.json`, using credentials appropriate
   to your local database. Do not commit database credentials.
3. Run the app. Migrations are applied and the database is seeded automatically on startup:
   ```bash
   cd RentalManagementApp
   dotnet run
   ```

The startup seed creates sample applicant and property-manager accounts for local development.
Their credentials are intentionally not documented here.

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
