using RentalManagementApp.Data.Entities;
using RentalManagementApp.Services.Interfaces;

namespace RentalManagementApp.Services;

public class UnitAvailabilityService : IUnitAvailabilityService
{
    public bool IsUnitAvailable(IEnumerable<Lease> leases, DateOnly asOfDate)
    {
        return leases.All(l => !l.CoversDate(asOfDate));
    }
}
