using RentalManagementApp.Data.Entities;
using RentalManagementApp.Services;
using Xunit;

namespace RentalManagementApp.Tests;

public class UnitAvailabilityServiceTests
{
    private readonly UnitAvailabilityService _sut = new();

    [Fact]
    public void IsUnitAvailable_ReturnsTrue_WhenUnitHasNoLeases()
    {
        var result = _sut.IsUnitAvailable(new List<Lease>(), new DateOnly(2026, 1, 1));
        Assert.True(result);
    }

    [Fact]
    public void IsUnitAvailable_ReturnsFalse_WhenALeaseCoversTheDate()
    {
        var leases = new List<Lease>
        {
            new() { StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 12, 31) }
        };

        var result = _sut.IsUnitAvailable(leases, new DateOnly(2026, 6, 15));
        Assert.False(result);
    }

    [Fact]
    public void IsUnitAvailable_ReturnsTrue_WhenAllLeasesHaveExpired()
    {
        var leases = new List<Lease>
        {
            new() { StartDate = new DateOnly(2020, 1, 1), EndDate = new DateOnly(2020, 12, 31) }
        };

        var result = _sut.IsUnitAvailable(leases, new DateOnly(2026, 1, 1));
        Assert.True(result);
    }

    [Theory]
    [InlineData("2026-01-01", true)]   // start date boundary - inclusive
    [InlineData("2026-12-31", true)]   // end date boundary - inclusive
    [InlineData("2025-12-31", false)]  // day before start - not covered
    [InlineData("2027-01-01", false)]  // day after end - not covered
    public void CoversDate_IsInclusiveOfBothBoundaries(string dateString, bool expectedCovered)
    {
        var lease = new Lease { StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 12, 31) };
        var date = DateOnly.Parse(dateString);

        Assert.Equal(expectedCovered, lease.CoversDate(date));
    }

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
