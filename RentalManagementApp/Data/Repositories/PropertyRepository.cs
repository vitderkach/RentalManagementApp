using Microsoft.EntityFrameworkCore;
using RentalManagementApp.Data;
using RentalManagementApp.Data.Entities;
using RentalManagementApp.Services.Interfaces;

namespace RentalManagementApp.Data.Repositories;

public class PropertyRepository(ApplicationDbContext db) : IPropertyRepository
{
    public async Task<IReadOnlyList<Property>> GetManagedPropertiesWithUnitsAsync(string managerId) =>
        await db.Properties
            .Where(p => p.PropertyManagerId == managerId)
            .Include(p => p.Units).ThenInclude(u => u.UnitType)
            .Include(p => p.Units).ThenInclude(u => u.Leases)
            .Include(p => p.Units).ThenInclude(u => u.RentalApplications)
            .OrderBy(p => p.Name)
            .ToListAsync();

    public async Task<IReadOnlyList<Unit>> GetAvailableUnitsAsync() =>
        await db.Units
            .Include(u => u.Property)
            .Include(u => u.UnitType)
            .Include(u => u.Leases)
            .Include(u => u.RentalApplications)
            .ToListAsync();

    public async Task<Property?> GetManagedPropertyAsync(int propertyId, string managerId) =>
        await db.Properties
            .FirstOrDefaultAsync(p => p.Id == propertyId && p.PropertyManagerId == managerId);

    public async Task<Unit?> GetUnitForPropertyAsync(int unitId, int propertyId) =>
        await db.Units.FirstOrDefaultAsync(u => u.Id == unitId && u.PropertyId == propertyId);

    public async Task<IReadOnlyList<UnitType>> GetSelectableUnitTypesAsync(int currentlySelectedId) =>
        await db.UnitTypes
            .Where(t => t.IsActive || t.Id == currentlySelectedId)
            .OrderBy(t => t.Name)
            .ToListAsync();
}
