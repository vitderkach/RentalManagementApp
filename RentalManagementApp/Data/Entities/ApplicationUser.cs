using Microsoft.AspNetCore.Identity;

namespace RentalManagementApp.Data.Entities;

/// <summary>
/// Identity user extended with the applicant/property-manager profile fields shared by both roles.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    public string FullName => $"{FirstName} {LastName}".Trim();

    public ICollection<Property> ManagedProperties { get; set; } = new List<Property>();
    public ICollection<RentalApplication> RentalApplications { get; set; } = new List<RentalApplication>();
}
