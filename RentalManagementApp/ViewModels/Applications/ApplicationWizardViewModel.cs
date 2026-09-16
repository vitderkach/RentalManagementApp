using System.ComponentModel.DataAnnotations;
using RentalManagementApp.Data.Entities;

namespace RentalManagementApp.ViewModels.Applications;

public enum WizardSection
{
    ApplicantInfo = 0,
    ResidenceHistory = 1,
    Summary = 2
}

/// <summary>
/// The single view model that drives the whole application wizard page. Each section is
/// rendered by its own view component/partial based on <see cref="CurrentSection"/>.
/// </summary>
public class ApplicationWizardViewModel
{
    public int ApplicationId { get; set; }
    public int UnitId { get; set; }
    public string UnitLabel { get; set; } = string.Empty;
    public ApplicationStatus Status { get; set; }
    public WizardSection CurrentSection { get; set; }
    public bool IsReadOnly { get; set; }
    public bool ApplicantInfoCompleted { get; set; }
    public bool ResidenceHistoryCompleted { get; set; }

    public ApplicantInfoInputModel ApplicantInfo { get; set; } = new();
    public List<ResidenceViewModel> Residences { get; set; } = new();

    public bool CanSubmit => !IsReadOnly && ApplicantInfoCompleted && ResidenceHistoryCompleted
                              && Status is ApplicationStatus.Draft or ApplicationStatus.Returned;
}

public class ApplicantInfoInputModel
{
    [Required, StringLength(100), Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100), Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Required, Phone, StringLength(30)]
    public string Phone { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(300), Display(Name = "Current address")]
    public string CurrentAddress { get; set; } = string.Empty;
}

public class ResidenceViewModel
{
    public int Id { get; set; }
    public string Address { get; set; } = string.Empty;
    public string LandlordName { get; set; } = string.Empty;
    public string LandlordPhone { get; set; } = string.Empty;
    public DateOnly MoveInDate { get; set; }
    public DateOnly? MoveOutDate { get; set; }
}

public class ResidenceFormViewModel
{
    public int? Id { get; set; }

    [Required]
    public int ApplicationId { get; set; }

    [Required, StringLength(300)]
    public string Address { get; set; } = string.Empty;

    [Required, StringLength(150), Display(Name = "Landlord name")]
    public string LandlordName { get; set; } = string.Empty;

    [Required, Phone, StringLength(30), Display(Name = "Landlord phone")]
    public string LandlordPhone { get; set; } = string.Empty;

    [Required, DataType(DataType.Date), Display(Name = "Move-in date")]
    public DateOnly MoveInDate { get; set; }

    [DataType(DataType.Date), Display(Name = "Move-out date")]
    public DateOnly? MoveOutDate { get; set; }
}
