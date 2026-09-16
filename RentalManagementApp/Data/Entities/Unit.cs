namespace RentalManagementApp.Data.Entities;

public class Unit
{
    public int Id { get; set; }
    public int PropertyId { get; set; }
    public Property? Property { get; set; }

    public string UnitNumber { get; set; } = string.Empty;
    public int Bedrooms { get; set; }
    public decimal MonthlyRent { get; set; }

    public int UnitTypeId { get; set; }
    public UnitType? UnitType { get; set; }

    public ICollection<Lease> Leases { get; set; } = new List<Lease>();
    public ICollection<RentalApplication> RentalApplications { get; set; } = new List<RentalApplication>();
}
