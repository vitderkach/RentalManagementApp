using RentalManagementApp.Domain.Entities;
using RentalManagementApp.Application.Services;

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
}
