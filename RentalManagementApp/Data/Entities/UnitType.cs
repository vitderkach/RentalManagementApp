namespace RentalManagementApp.Data.Entities;

/// <summary>
/// Lookup table for unit types (e.g. Studio, One Bedroom). Inactive types remain assignable to
/// units that already use them, but cannot be selected for other units.
/// </summary>
public class UnitType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<Unit> Units { get; set; } = new List<Unit>();
}
