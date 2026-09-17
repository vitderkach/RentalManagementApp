using Microsoft.EntityFrameworkCore;
using RentalManagementApp.Data.Entities;
using RentalManagementApp.Data.Enums;
using RentalManagementApp.Services;
using RentalManagementApp.Services.Interfaces;
using Xunit;

namespace RentalManagementApp.Tests;

public class PropertyManagementServiceTests
{
    [Fact]
    public async Task SaveUnitAsync_Fails_WhenCreatingNewUnit_WithInactiveUnitType()
    {
        using var db = TestDbFactory.Create();
        var (_, property) = TestDbFactory.SeedPropertyAndUnit(db);
        var inactiveType = new UnitType { Name = "Loft (Discontinued)", IsActive = false };
        db.UnitTypes.Add(inactiveType);
        await db.SaveChangesAsync();

        var sut = new PropertyManagementService(db);
        var result = await sut.SaveUnitAsync(property.Id, "manager-1",
            new UnitInput(null, "202", 2, 1400m, inactiveType.Id));

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task SaveUnitAsync_AllowsKeepingExistingInactiveUnitType_OnUpdate()
    {
        using var db = TestDbFactory.Create();
        var inactiveType = new UnitType { Name = "Loft (Discontinued)", IsActive = false };
        db.UnitTypes.Add(inactiveType);

        var property = new Property
        {
            Name = "P", AddressLine1 = "1 St", City = "C", State = "S", ZipCode = "0",
            PropertyManagerId = "manager-1"
        };
        db.Properties.Add(property);

        var unit = new Unit { Property = property, UnitNumber = "1", Bedrooms = 1, MonthlyRent = 1000m, UnitType = inactiveType };
        db.Units.Add(unit);
        await db.SaveChangesAsync();

        var sut = new PropertyManagementService(db);
        // Update other fields but keep the same (inactive) unit type - should be allowed.
        var result = await sut.SaveUnitAsync(property.Id, "manager-1",
            new UnitInput(unit.Id, "1A", 1, 1050m, inactiveType.Id));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task SaveUnitAsync_Fails_WhenSwitchingExistingUnit_ToADifferentInactiveType()
    {
        using var db = TestDbFactory.Create();
        var activeType = new UnitType { Name = "Studio", IsActive = true };
        var inactiveType = new UnitType { Name = "Loft (Discontinued)", IsActive = false };
        db.UnitTypes.AddRange(activeType, inactiveType);

        var property = new Property
        {
            Name = "P", AddressLine1 = "1 St", City = "C", State = "S", ZipCode = "0",
            PropertyManagerId = "manager-1"
        };
        db.Properties.Add(property);

        var unit = new Unit { Property = property, UnitNumber = "1", Bedrooms = 1, MonthlyRent = 1000m, UnitType = activeType };
        db.Units.Add(unit);
        await db.SaveChangesAsync();

        var sut = new PropertyManagementService(db);
        var result = await sut.SaveUnitAsync(property.Id, "manager-1",
            new UnitInput(unit.Id, "1", 1, 1000m, inactiveType.Id));

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task SaveUnitAsync_Succeeds_WhenCreatingNewUnit_WithActiveUnitType()
    {
        using var db = TestDbFactory.Create();
        var (_, property) = TestDbFactory.SeedPropertyAndUnit(db);
        var activeType = await db.UnitTypes.FirstAsync();

        var sut = new PropertyManagementService(db);
        var result = await sut.SaveUnitAsync(property.Id, "manager-1",
            new UnitInput(null, "303", 2, 1600m, activeType.Id));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task DeleteUnitAsync_Fails_WhenUnitHasRentalApplications()
    {
        using var db = TestDbFactory.Create();
        var (unit, _) = TestDbFactory.SeedPropertyAndUnit(db);
        db.RentalApplications.Add(new RentalApplication { UnitId = unit.Id, ApplicantId = "applicant-1", Status = ApplicationStatus.Draft });
        await db.SaveChangesAsync();

        var sut = new PropertyManagementService(db);
        var result = await sut.DeleteUnitAsync(unit.Id, "manager-1");

        Assert.False(result.Succeeded);
    }
}
