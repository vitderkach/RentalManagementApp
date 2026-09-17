using RentalManagementApp.Data.Entities;
using RentalManagementApp.Services;
using Xunit;

namespace RentalManagementApp.Tests;

public class LeaseFactoryTests
{
    private readonly LeaseFactory _sut = new();

    [Fact]
    public void CreateLeaseForApproval_BuildsATwelveMonthTerm_StartingOnGivenDate()
    {
        var application = new RentalApplication
        {
            Id = 42,
            UnitId = 7,
            Unit = new Unit { Id = 7, MonthlyRent = 1500m }
        };
        var startDate = new DateOnly(2026, 3, 1);

        var lease = _sut.CreateLeaseForApproval(application, startDate);

        Assert.Equal(7, lease.UnitId);
        Assert.Equal(42, lease.RentalApplicationId);
        Assert.Equal(startDate, lease.StartDate);
        Assert.Equal(new DateOnly(2027, 2, 28), lease.EndDate);
        Assert.Equal(1500m, lease.MonthlyRent);
    }

    [Fact]
    public void CreateLeaseForApproval_Throws_WhenUnitNotLoaded()
    {
        var application = new RentalApplication { Id = 1, UnitId = 7 };

        Assert.Throws<InvalidOperationException>(() =>
            _sut.CreateLeaseForApproval(application, new DateOnly(2026, 1, 1)));
    }
}
