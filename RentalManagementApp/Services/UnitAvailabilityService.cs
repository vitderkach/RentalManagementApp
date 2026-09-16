using RentalManagementApp.Data.Entities;
using RentalManagementApp.Services.Interfaces;

namespace RentalManagementApp.Services;

public class UnitAvailabilityService : IUnitAvailabilityService
{
    public bool IsUnitAvailable(IEnumerable<Lease> leases, DateOnly asOfDate)
    {
        return leases.All(l => !l.CoversDate(asOfDate));
    }

    public Lease CreateLeaseForApproval(RentalApplication application, DateOnly startDate)
    {
        if (application.Unit is null)
        {
            throw new InvalidOperationException("Unit must be loaded to create a lease.");
        }

        return new Lease
        {
            UnitId = application.UnitId,
            RentalApplicationId = application.Id,
            StartDate = startDate,
            EndDate = startDate.AddMonths(12).AddDays(-1),
            MonthlyRent = application.Unit.MonthlyRent
        };
    }
}
