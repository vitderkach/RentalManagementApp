using Microsoft.AspNetCore.Identity;
using RentalManagementApp.Domain.Entities;

namespace RentalManagementApp.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    public string FullName => $"{FirstName} {LastName}".Trim();

    public ICollection<Property> ManagedProperties { get; set; } = new List<Property>();
    public ICollection<RentalApplication> RentalApplications { get; set; } = new List<RentalApplication>();
}
