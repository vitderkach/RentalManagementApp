using Microsoft.EntityFrameworkCore;
using RentalManagementApp.Infrastructure.Data;
using RentalManagementApp.Domain.Entities;

namespace RentalManagementApp.Tests;

public static class TestDbFactory
{
    public static ApplicationDbContext Create(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    public static (Unit Unit, Property Property) SeedPropertyAndUnit(ApplicationDbContext db, decimal rent = 1200m)
    {
        var unitType = new UnitType { Name = "One Bedroom", IsActive = true };
        db.UnitTypes.Add(unitType);

        var property = new Property
        {
            Name = "Test Property",
            AddressLine1 = "1 Main St",
            City = "Testville",
            State = "TS",
            ZipCode = "00000",
            PropertyManagerId = "manager-1"
        };
        db.Properties.Add(property);

        var unit = new Unit
        {
            Property = property,
            UnitNumber = "101",
            Bedrooms = 1,
            MonthlyRent = rent,
            UnitType = unitType
        };
        db.Units.Add(unit);
        db.SaveChanges();

        return (unit, property);
    }
}
