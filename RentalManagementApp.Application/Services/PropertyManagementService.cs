using RentalManagementApp.Domain.Entities;
using RentalManagementApp.Application.Interfaces;

namespace RentalManagementApp.Application.Services;

public class PropertyManagementService : IPropertyManagementService
{
    private readonly IPropertyRepository _properties;

    public PropertyManagementService(IPropertyRepository properties)
    {
        _properties = properties;
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
        _properties.Add(property);
        await _properties.SaveChangesAsync();
        return ServiceResult<int>.Success(property.Id);
    }

    public async Task<ServiceResult> UpdatePropertyAsync(int propertyId, string managerId, string name, string addressLine1, string? addressLine2, string city, string state, string zip)
    {
        var property = await _properties.GetManagedPropertyAsync(propertyId, managerId);
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

        await _properties.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DeletePropertyAsync(int propertyId, string managerId)
    {
        var property = await _properties.GetManagedPropertyWithApplicationsAsync(propertyId, managerId);

        if (property is null)
        {
            return ServiceResult.Failure("Property not found.");
        }

        if (property.Units.Any(u => u.RentalApplications.Count != 0))
        {
            return ServiceResult.Failure("Cannot remove a property whose units have rental applications.");
        }

        _properties.Remove(property);
        await _properties.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult<int>> SaveUnitAsync(int propertyId, string managerId, UnitInput input)
    {
        var property = await _properties.GetManagedPropertyAsync(propertyId, managerId);
        if (property is null)
        {
            return ServiceResult<int>.Failure("Property not found.");
        }

        var unitType = await _properties.GetUnitTypeAsync(input.UnitTypeId);
        if (unitType is null)
        {
            return ServiceResult<int>.Failure("Unit type not found.");
        }

        Unit unit;
        if (input.Id is int id)
        {
            var existing = await _properties.GetUnitForPropertyAsync(id, propertyId);
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
            _properties.Add(unit);
        }

        await _properties.SaveChangesAsync();
        return ServiceResult<int>.Success(unit.Id);
    }

    public async Task<ServiceResult> DeleteUnitAsync(int unitId, string managerId)
    {
        var unit = await _properties.GetManagedUnitWithApplicationsAsync(unitId, managerId);

        if (unit is null)
        {
            return ServiceResult.Failure("Unit not found.");
        }

        if (unit.RentalApplications.Count != 0)
        {
            return ServiceResult.Failure("Cannot remove a unit that has rental applications.");
        }

        _properties.Remove(unit);
        await _properties.SaveChangesAsync();
        return ServiceResult.Success();
    }
}
