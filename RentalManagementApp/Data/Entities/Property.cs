namespace RentalManagementApp.Data.Entities;

public class Property
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;

    public string PropertyManagerId { get; set; } = string.Empty;
    public ApplicationUser? PropertyManager { get; set; }

    public ICollection<Unit> Units { get; set; } = new List<Unit>();
}
