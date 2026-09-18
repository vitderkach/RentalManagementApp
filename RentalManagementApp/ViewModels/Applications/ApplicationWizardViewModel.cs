using System.ComponentModel.DataAnnotations;
using RentalManagementApp.Data.Entities;
using RentalManagementApp.Data.Enums;
using RentalManagementApp.ViewModels.Applications.Enums;

namespace RentalManagementApp.ViewModels.Applications;

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
    [RegularExpression(@"^\+?[0-9\s().-]{7,30}$", ErrorMessage = "Enter a valid phone number.")]
    public string Phone { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(300), Display(Name = "Current address")]
    public string CurrentAddress { get; set; } = string.Empty;

    [Required, DataType(DataType.Date), Display(Name = "Desired lease start date")]
    public DateOnly DesiredLeaseStartDate { get; set; }
}

public class ResidenceViewModel
{
    public int Id { get; set; }
    public bool IsPending { get; set; }
    public int? PendingIndex { get; set; }
    public string Address { get; set; } = string.Empty;
    public string LandlordName { get; set; } = string.Empty;
    public string LandlordPhone { get; set; } = string.Empty;
    public DateOnly MoveInDate { get; set; }
    public DateOnly? MoveOutDate { get; set; }
}

public class ResidenceFormViewModel : IValidatableObject
{
    public int? Id { get; set; }
    public int? PendingIndex { get; set; }

    [Required]
    public int ApplicationId { get; set; }

    [Required, StringLength(300)]
    public string Address { get; set; } = string.Empty;

    [Required, StringLength(150), Display(Name = "Landlord name")]
    public string LandlordName { get; set; } = string.Empty;

    [Required, Phone, StringLength(30), Display(Name = "Landlord phone")]
    [RegularExpression(@"^\+?[0-9\s().-]{7,30}$", ErrorMessage = "Enter a valid phone number.")]
    public string LandlordPhone { get; set; } = string.Empty;

    [Required, DataType(DataType.Date), Display(Name = "Move-in date")]
    public DateOnly MoveInDate { get; set; }

    [DataType(DataType.Date), Display(Name = "Move-out date")]
    public DateOnly? MoveOutDate { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MoveOutDate is { } moveOut && moveOut < MoveInDate)
        {
            yield return new ValidationResult(
                "Move-out date cannot be earlier than the move-in date.",
                new[] { nameof(MoveOutDate) });
        }
    }
}
