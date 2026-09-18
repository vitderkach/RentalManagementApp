using RentalManagementApp.Data.Entities;

namespace RentalManagementApp.Services.Interfaces;

public interface IPropertyRepository
{
    Task<IReadOnlyList<Property>> GetManagedPropertiesWithUnitsAsync(string managerId);
    Task<IReadOnlyList<Unit>> GetAvailableUnitsAsync();
    Task<Property?> GetManagedPropertyAsync(int propertyId, string managerId);
    Task<Unit?> GetUnitForPropertyAsync(int unitId, int propertyId);
    Task<IReadOnlyList<UnitType>> GetSelectableUnitTypesAsync(int currentlySelectedId);
}
