using Microsoft.EntityFrameworkCore;
using RentalManagementApp.Data;
using RentalManagementApp.Data.Entities;
using RentalManagementApp.Services.Interfaces;

namespace RentalManagementApp.Services;

public class PropertyManagementService : IPropertyManagementService
{
    private readonly ApplicationDbContext _db;

    public PropertyManagementService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ServiceResult<int>> CreatePropertyAsync(string managerId, string name, string addressLine1, string? addressLine2, string city, string state, string zip)
    {
        var property = new Property
        {
            PropertyManagerId = managerId,
            Name = name,
            AddressLine1 = addressLine1,
            AddressLine2 = addressLine2,
            City = city,
            State = state,
            ZipCode = zip
        };
        _db.Properties.Add(property);
        await _db.SaveChangesAsync();
        return ServiceResult<int>.Success(property.Id);
    }

    public async Task<ServiceResult> UpdatePropertyAsync(int propertyId, string managerId, string name, string addressLine1, string? addressLine2, string city, string state, string zip)
    {
        var property = await _db.Properties.FirstOrDefaultAsync(p => p.Id == propertyId && p.PropertyManagerId == managerId);
        if (property is null)
        {
            return ServiceResult.Failure("Property not found.");
        }

        property.Name = name;
        property.AddressLine1 = addressLine1;
        property.AddressLine2 = addressLine2;
        property.City = city;
        property.State = state;
        property.ZipCode = zip;

        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DeletePropertyAsync(int propertyId, string managerId)
    {
        var property = await _db.Properties
            .Include(p => p.Units)
            .ThenInclude(u => u.RentalApplications)
            .FirstOrDefaultAsync(p => p.Id == propertyId && p.PropertyManagerId == managerId);

        if (property is null)
        {
            return ServiceResult.Failure("Property not found.");
        }

        if (property.Units.Any(u => u.RentalApplications.Count != 0))
        {
            return ServiceResult.Failure("Cannot remove a property whose units have rental applications.");
        }

        _db.Properties.Remove(property);
        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult<int>> SaveUnitAsync(int propertyId, string managerId, UnitInput input)
    {
        var property = await _db.Properties.FirstOrDefaultAsync(p => p.Id == propertyId && p.PropertyManagerId == managerId);
        if (property is null)
        {
            return ServiceResult<int>.Failure("Property not found.");
        }

        var unitType = await _db.UnitTypes.FirstOrDefaultAsync(t => t.Id == input.UnitTypeId);
        if (unitType is null)
        {
            return ServiceResult<int>.Failure("Unit type not found.");
        }

        Unit unit;
        if (input.Id is int id)
        {
            var existing = await _db.Units.FirstOrDefaultAsync(u => u.Id == id && u.PropertyId == propertyId);
            if (existing is null)
            {
                return ServiceResult<int>.Failure("Unit not found.");
            }
            
            if (!unitType.IsActive && existing.UnitTypeId != unitType.Id)
            {
                return ServiceResult<int>.Failure("An inactive unit type cannot be selected.");
            }

            existing.UnitNumber = input.UnitNumber;
            existing.Bedrooms = input.Bedrooms;
            existing.MonthlyRent = input.MonthlyRent;
            existing.UnitTypeId = input.UnitTypeId;
            unit = existing;
        }
        else
        {
            if (!unitType.IsActive)
            {
                return ServiceResult<int>.Failure("An inactive unit type cannot be selected.");
            }

            unit = new Unit
            {
                PropertyId = propertyId,
                UnitNumber = input.UnitNumber,
                Bedrooms = input.Bedrooms,
                MonthlyRent = input.MonthlyRent,
                UnitTypeId = input.UnitTypeId
            };
            _db.Units.Add(unit);
        }

        await _db.SaveChangesAsync();
        return ServiceResult<int>.Success(unit.Id);
    }

    public async Task<ServiceResult> DeleteUnitAsync(int unitId, string managerId)
    {
        var unit = await _db.Units
            .Include(u => u.Property)
            .Include(u => u.RentalApplications)
            .FirstOrDefaultAsync(u => u.Id == unitId && u.Property!.PropertyManagerId == managerId);

        if (unit is null)
        {
            return ServiceResult.Failure("Unit not found.");
        }

        if (unit.RentalApplications.Count != 0)
        {
            return ServiceResult.Failure("Cannot remove a unit that has rental applications.");
        }

        _db.Units.Remove(unit);
        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }
}
