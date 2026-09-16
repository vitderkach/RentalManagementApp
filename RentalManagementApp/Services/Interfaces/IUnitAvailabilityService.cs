using RentalManagementApp.Data.Entities;

namespace RentalManagementApp.Services.Interfaces;

/// <summary>
/// Pure, side-effect-free rules for unit availability. Kept independent of EF Core so the
/// business logic can be unit tested in isolation.
/// </summary>
public interface IUnitAvailabilityService
{
    /// <summary>A unit is unavailable while any of its leases covers the given date.</summary>
    bool IsUnitAvailable(IEnumerable<Lease> leases, DateOnly asOfDate);

    /// <summary>Builds the twelve-month lease created when an application is approved.</summary>
    Lease CreateLeaseForApproval(RentalApplication application, DateOnly startDate);
}
