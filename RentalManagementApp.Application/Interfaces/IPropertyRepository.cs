using RentalManagementApp.Domain.Entities;

namespace RentalManagementApp.Application.Interfaces;

public interface IPropertyRepository
{
    Task<IReadOnlyList<Property>> GetManagedPropertiesWithUnitsAsync(string managerId);
    Task<IReadOnlyList<Unit>> GetAvailableUnitsAsync();
    Task<Property?> GetManagedPropertyAsync(int propertyId, string managerId);
    Task<Unit?> GetUnitForPropertyAsync(int unitId, int propertyId);
    Task<IReadOnlyList<UnitType>> GetSelectableUnitTypesAsync(int currentlySelectedId);
    Task<Property?> GetManagedPropertyWithApplicationsAsync(int propertyId, string managerId);
    Task<UnitType?> GetUnitTypeAsync(int unitTypeId);
    Task<Unit?> GetManagedUnitWithApplicationsAsync(int unitId, string managerId);
    void Add(Property property);
    void Add(Unit unit);
    void Remove(Property property);
    void Remove(Unit unit);
    Task SaveChangesAsync();
}
