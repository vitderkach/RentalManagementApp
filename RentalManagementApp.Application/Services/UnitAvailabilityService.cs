using RentalManagementApp.Domain.Entities;
using RentalManagementApp.Application.Interfaces;

namespace RentalManagementApp.Application.Services;

public class UnitAvailabilityService : IUnitAvailabilityService
{
    public bool IsUnitAvailable(IEnumerable<Lease> leases, DateOnly asOfDate)
    {
        return leases.All(l => !l.CoversDate(asOfDate));
    }
}
