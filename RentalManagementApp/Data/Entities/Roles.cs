namespace RentalManagementApp.Data.Entities;

/// <summary>
/// The two roles supported by the system. Kept as constants so controllers/views can reference
/// them without magic strings.
/// </summary>
public static class Roles
{
    public const string Applicant = "Applicant";
    public const string PropertyManager = "PropertyManager";
}
