using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using RentalManagementApp.Domain.Entities;
using RentalManagementApp.Domain.Enums;
using RentalManagementApp.Application.Contracts;

namespace RentalManagementApp.ViewModels.Applications;

public class ApplicationListItemViewModel
{
    public int Id { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public string UnitNumber { get; set; } = string.Empty;
    public string ApplicantName { get; set; } = string.Empty;
    public ApplicationStatus Status { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ApplicationListViewModel
{
    public bool IsManager { get; set; }
    public List<ApplicationListItemViewModel> Items { get; set; } = new();

    public ApplicationStatus? StatusFilter { get; set; }
    public int? PropertyFilter { get; set; }

    public IEnumerable<SelectListItem> StatusOptions { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> PropertyOptions { get; set; } = new List<SelectListItem>();
}

public class ApplicationStatusHistoryItemViewModel
{
    public ApplicationStatus Status { get; set; }
    public string ChangedByName { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public string? Comment { get; set; }
}

public class ApplicationDetailsViewModel
{
    public int Id { get; set; }
    public ApplicationStatus Status { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public string UnitNumber { get; set; } = string.Empty;
    public string ApplicantName { get; set; } = string.Empty;
    public string ApplicantEmail { get; set; } = string.Empty;
    public string ApplicantPhone { get; set; } = string.Empty;
    public string CurrentAddress { get; set; } = string.Empty;
    public DateOnly? DesiredLeaseStartDate { get; set; }
    public List<ResidenceViewModel> Residences { get; set; } = new();
    public List<ApplicationStatusHistoryItemViewModel> StatusHistory { get; set; } = new();
    public bool CanManagerReview { get; set; }
    public bool CanApplicantEdit { get; set; }
    public bool CanApplicantWithdraw { get; set; }
}

public class ReviewFormViewModel
{
    [Required]
    public int ApplicationId { get; set; }

    [Required]
    public ApplicationReviewOutcome Outcome { get; set; }

    [StringLength(1000)]
    public string? Comment { get; set; }
}
