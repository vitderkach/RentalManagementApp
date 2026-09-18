using Microsoft.EntityFrameworkCore;
using RentalManagementApp.Infrastructure.Data;
using RentalManagementApp.Domain.Entities;
using RentalManagementApp.Application.Interfaces;

namespace RentalManagementApp.Infrastructure.Data.Repositories;

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

    public async Task<Property?> GetManagedPropertyWithApplicationsAsync(int propertyId, string managerId) =>
        await db.Properties
            .Include(p => p.Units)
            .ThenInclude(u => u.RentalApplications)
            .FirstOrDefaultAsync(p => p.Id == propertyId && p.PropertyManagerId == managerId);

    public Task<UnitType?> GetUnitTypeAsync(int unitTypeId) =>
        db.UnitTypes.FirstOrDefaultAsync(t => t.Id == unitTypeId);

    public async Task<Unit?> GetManagedUnitWithApplicationsAsync(int unitId, string managerId) =>
        await db.Units
            .Include(u => u.Property)
            .Include(u => u.RentalApplications)
            .FirstOrDefaultAsync(u => u.Id == unitId && u.Property!.PropertyManagerId == managerId);

    public void Add(Property property) => db.Properties.Add(property);

    public void Add(Unit unit) => db.Units.Add(unit);

    public void Remove(Property property) => db.Properties.Remove(property);

    public void Remove(Unit unit) => db.Units.Remove(unit);

    public Task SaveChangesAsync() => db.SaveChangesAsync();
}
