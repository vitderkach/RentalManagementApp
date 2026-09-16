using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace RentalManagementApp.ViewModels.Properties;

public class PropertyFormViewModel
{
    public int? Id { get; set; }

    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(200), Display(Name = "Address line 1")]
    public string AddressLine1 { get; set; } = string.Empty;

    [StringLength(200), Display(Name = "Address line 2")]
    public string? AddressLine2 { get; set; }

    [Required, StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string State { get; set; } = string.Empty;

    [Required, StringLength(20), Display(Name = "Zip code")]
    public string ZipCode { get; set; } = string.Empty;
}

public class UnitFormViewModel
{
    public int? Id { get; set; }

    [Required]
    public int PropertyId { get; set; }

    [Required, StringLength(20), Display(Name = "Unit number")]
    public string UnitNumber { get; set; } = string.Empty;

    [Required, Range(0, 10)]
    public int Bedrooms { get; set; }

    [Required, Range(0, 100000), Display(Name = "Monthly rent")]
    public decimal MonthlyRent { get; set; }

    [Required, Display(Name = "Unit type")]
    public int UnitTypeId { get; set; }

    public IEnumerable<SelectListItem> UnitTypeOptions { get; set; } = new List<SelectListItem>();
}

public class PropertyDetailViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public List<UnitRowViewModel> Units { get; set; } = new();
}

public class UnitRowViewModel
{
    public int Id { get; set; }
    public int PropertyId { get; set; }
    public string UnitNumber { get; set; } = string.Empty;
    public int Bedrooms { get; set; }
    public decimal MonthlyRent { get; set; }
    public string UnitTypeName { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public bool HasApplications { get; set; }
}

public class PropertiesIndexViewModel
{
    public List<PropertyDetailViewModel> Properties { get; set; } = new();
}

public class AvailableUnitViewModel
{
    public int UnitId { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string UnitNumber { get; set; } = string.Empty;
    public int Bedrooms { get; set; }
    public decimal MonthlyRent { get; set; }
    public string UnitTypeName { get; set; } = string.Empty;
    public bool HasOpenApplicationForCurrentUser { get; set; }
}

public class UnitTypeFormViewModel
{
    public int? Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}
