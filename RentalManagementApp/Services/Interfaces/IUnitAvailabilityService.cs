using RentalManagementApp.Data.Entities;

namespace RentalManagementApp.Services.Interfaces;

public interface IUnitAvailabilityService
{
    bool IsUnitAvailable(IEnumerable<Lease> leases, DateOnly asOfDate);
}
