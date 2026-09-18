# Property Rental Management System

A full-stack ASP.NET Core MVC (.NET 10) application for a property management company that
accepts rental applications for its apartments.

## Demo

[Watch the screen recording](https://drive.google.com/file/d/1KGduGQiFH1mAAQd_6Kw9iq1GbvU5aGWn/view?usp=sharing)

## Stack

- ASP.NET Core MVC + Razor views, partial views, and view components
- ASP.NET Core Identity (roles: `Applicant`, `PropertyManager`)
- Entity Framework Core (SQL Server), code-first migrations
- [Bogus](https://github.com/bchavez/Bogus) for idempotent startup seeding
- xUnit for business-logic unit tests

## Running locally

1. Start a local SQL Server instance.
2. Configure the `ConnectionStrings:DefaultConnection` value in
   `RentalManagementApp.Web/appsettings.Development.json`, using credentials appropriate
   to your local database. Do not commit database credentials.
3. Run the app. Migrations are applied and the database is seeded automatically on startup:
   ```bash
   cd RentalManagementApp.Web
   dotnet run
   ```

The startup seed creates sample applicant and property-manager accounts for local development.
Their credentials are intentionally not documented here.

## Tests

```bash
dotnet test
```

## Clean Architecture layout

- `RentalManagementApp.Domain` - business entities, enums, roles, and domain rules.
- `RentalManagementApp.Application` - use-case services, result types, inputs, and repository ports.
- `RentalManagementApp.Infrastructure` - EF Core `ApplicationDbContext`, configurations,
  migrations, seed data, repository adapters, and the ASP.NET Core Identity user/store type.
- `RentalManagementApp.Web` - ASP.NET Core MVC composition root, controllers, Razor views,
  view models, view components, and static assets.
- `RentalManagementApp.Tests` - application-service tests using the infrastructure in-memory
  EF Core adapter.

Dependencies point inward: `Application` depends on `Domain`, `Infrastructure` depends on
`Application` and `Domain`, and `Web` composes `Application` with `Infrastructure`.
`ApplicationUser` remains infrastructure-specific, and
domain entities keep only user-id foreign keys, preventing a dependency back into Infrastructure.
