using RentalManagementApp.Domain.Entities;

namespace RentalManagementApp.Application.Interfaces;

public interface IUnitAvailabilityService
{
    bool IsUnitAvailable(IEnumerable<Lease> leases, DateOnly asOfDate);
}
