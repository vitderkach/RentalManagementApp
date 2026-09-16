namespace RentalManagementApp.Data.Entities;

/// <summary>
/// A twelve-month lease created automatically when a rental application is approved.
/// A unit is unavailable while any lease's term (StartDate..EndDate) covers today.
/// </summary>
public class Lease
{
    public int Id { get; set; }

    public int UnitId { get; set; }
    public Unit? Unit { get; set; }

    public int RentalApplicationId { get; set; }
    public RentalApplication? RentalApplication { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal MonthlyRent { get; set; }

    public bool CoversDate(DateOnly date) => date >= StartDate && date <= EndDate;
}
