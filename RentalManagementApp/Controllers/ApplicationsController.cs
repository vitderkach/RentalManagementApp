using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RentalManagementApp.Data;
using RentalManagementApp.Data.Entities;
using RentalManagementApp.Data.Enums;
using RentalManagementApp.Services.Contracts;
using RentalManagementApp.Services.Interfaces;
using RentalManagementApp.ViewModels.Applications;
using RentalManagementApp.ViewModels.Applications.Enums;

namespace RentalManagementApp.Controllers;

[Authorize]
public class ApplicationsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IApplicantApplicationService _applicantApplications;
    private readonly IApplicationReviewService _applicationReviews;
    private readonly UserManager<ApplicationUser> _userManager;

    public ApplicationsController(
        ApplicationDbContext db,
        IApplicantApplicationService applicantApplications,
        IApplicationReviewService applicationReviews,
        UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _applicantApplications = applicantApplications;
        _applicationReviews = applicationReviews;
        _userManager = userManager;
    }

    private bool IsManager => User.IsInRole(Roles.PropertyManager);

    public async Task<IActionResult> Index(ApplicationStatus? status, int? propertyId)
    {
        var userId = _userManager.GetUserId(User)!;
        var query = _db.RentalApplications
            .Include(a => a.Unit).ThenInclude(u => u!.Property)
            .Include(a => a.Applicant)
            .AsQueryable();

        if (!IsManager)
        {
            query = query.Where(a => a.ApplicantId == userId);
        }

        if (status is not null)
        {
            query = query.Where(a => a.Status == status);
        }

        if (propertyId is not null)
        {
            query = query.Where(a => a.Unit!.PropertyId == propertyId);
        }

        var items = await query
            .OrderByDescending(a => a.UpdatedAt)
            .Select(a => new ApplicationListItemViewModel
            {
                Id = a.Id,
                PropertyName = a.Unit!.Property!.Name,
                UnitNumber = a.Unit.UnitNumber,
                ApplicantName = a.ApplicantFirstName + " " + a.ApplicantLastName,
                Status = a.Status,
                UpdatedAt = a.UpdatedAt
            })
            .ToListAsync();

        var propertyOptions = await _db.Properties
            .OrderBy(p => p.Name)
            .Select(p => new SelectListItem(p.Name, p.Id.ToString()))
            .ToListAsync();

        var vm = new ApplicationListViewModel
        {
            IsManager = IsManager,
            Items = items,
            StatusFilter = status,
            PropertyFilter = propertyId,
            StatusOptions = Enum.GetValues<ApplicationStatus>()
                .Select(s => new SelectListItem(s.ToString(), ((int)s).ToString())),
            PropertyOptions = propertyOptions
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "RequireApplicant")]
    public async Task<IActionResult> Start(int unitId)
    {
        var userId = _userManager.GetUserId(User)!;
        var result = await _applicantApplications.StartApplicationAsync(unitId, userId);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction("Browse", "Properties");
        }

        return RedirectToAction(nameof(Wizard), new { id = result.Value });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "RequireApplicant")]
    public async Task<IActionResult> Withdraw(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var result = await _applicantApplications.WithdrawAsync(id, userId);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Error;
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var application = await _db.RentalApplications
            .Include(a => a.Unit).ThenInclude(u => u!.Property)
            .Include(a => a.Residences)
            .Include(a => a.StatusHistory).ThenInclude(h => h.ChangedByUser)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (application is null) return NotFound();
        if (!IsManager && application.ApplicantId != userId) return Forbid();

        var vm = new ApplicationDetailsViewModel
        {
            Id = application.Id,
            Status = application.Status,
            PropertyName = application.Unit!.Property!.Name,
            UnitNumber = application.Unit.UnitNumber,
            ApplicantName = $"{application.ApplicantFirstName} {application.ApplicantLastName}",
            ApplicantEmail = application.ApplicantEmail,
            ApplicantPhone = application.ApplicantPhone,
            CurrentAddress = application.CurrentAddress,
            DesiredLeaseStartDate = application.DesiredLeaseStartDate,
            Residences = application.Residences.Select(MapResidence).ToList(),
            StatusHistory = application.StatusHistory
                .OrderBy(h => h.ChangedAt)
                .Select(h => new ApplicationStatusHistoryItemViewModel
                {
                    Status = h.Status,
                    ChangedByName = h.ChangedByUser is null ? "System" : h.ChangedByUser.FullName,
                    ChangedAt = h.ChangedAt,
                    Comment = h.Comment
                }).ToList(),
            CanManagerReview = IsManager && application.Status == ApplicationStatus.Submitted,
            CanApplicantEdit = !IsManager && application.ApplicantId == userId && application.Status.IsEditable(),
            CanApplicantWithdraw = !IsManager && application.ApplicantId == userId && !application.Status.IsTerminal()
        };

        return View(vm);
    }

    private static ResidenceViewModel MapResidence(Residence r) => new()
    {
        Id = r.Id,
        Address = r.Address,
        LandlordName = r.LandlordName,
        LandlordPhone = r.LandlordPhone,
        MoveInDate = r.MoveInDate,
        MoveOutDate = r.MoveOutDate
    };

    [Authorize(Policy = "RequireApplicant")]
    public async Task<IActionResult> Wizard(int id, WizardSection? section = null)
    {
        var vm = await BuildWizardViewModelAsync(id, section);
        if (vm is null) return NotFound();
        return View(vm);
    }

    private async Task<ApplicationWizardViewModel?> BuildWizardViewModelAsync(int id, WizardSection? forcedSection)
    {
        var userId = _userManager.GetUserId(User)!;
        var application = await _db.RentalApplications
            .Include(a => a.Unit).ThenInclude(u => u!.Property)
            .Include(a => a.Residences)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (application is null || application.ApplicantId != userId) return null;

        var isReadOnly = !application.Status.IsEditable();
        var defaultSection = isReadOnly
            ? WizardSection.Summary
            : (application.ApplicantInfoCompleted ? WizardSection.ResidenceHistory : WizardSection.ApplicantInfo);

        return new ApplicationWizardViewModel
        {
            ApplicationId = application.Id,
            UnitId = application.UnitId,
            UnitLabel = $"{application.Unit!.Property!.Name} - Unit {application.Unit.UnitNumber}",
            Status = application.Status,
            CurrentSection = forcedSection ?? defaultSection,
            IsReadOnly = isReadOnly,
            ApplicantInfoCompleted = application.ApplicantInfoCompleted,
            ResidenceHistoryCompleted = application.ResidenceHistoryCompleted,
            ApplicantInfo = new ApplicantInfoInputModel
            {
                FirstName = application.ApplicantFirstName,
                LastName = application.ApplicantLastName,
                Phone = application.ApplicantPhone,
                Email = application.ApplicantEmail,
                CurrentAddress = application.CurrentAddress,
                DesiredLeaseStartDate = application.DesiredLeaseStartDate
                    ?? DateOnly.FromDateTime(DateTime.UtcNow)
            },
            Residences = application.Residences.Select(MapResidence).ToList()
        };
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "RequireApplicant")]
    public async Task<IActionResult> WizardStep(int applicationId, WizardSection currentSection, ApplicantInfoInputModel applicantInfo, string action)
    {
        var userId = _userManager.GetUserId(User)!;

        if (action == "back")
        {
            var previous = currentSection switch
            {
                WizardSection.ResidenceHistory => WizardSection.ApplicantInfo,
                WizardSection.Summary => WizardSection.ResidenceHistory,
                _ => WizardSection.ApplicantInfo
            };
            return RedirectToAction(nameof(Wizard), new { id = applicationId, section = previous });
        }

        if (currentSection == WizardSection.ApplicantInfo)
        {
            ModelState.Clear();
            if (!TryValidateModel(applicantInfo, nameof(applicantInfo)))
            {
                var vm = await BuildWizardViewModelAsync(applicationId, WizardSection.ApplicantInfo);
                if (vm is null) return NotFound();
                vm.ApplicantInfo = applicantInfo;
                Response.StatusCode = 400;
                return View(nameof(Wizard), vm);
            }

            var result = await _applicantApplications.SaveApplicantInfoAsync(applicationId, userId,
                new ApplicantInfoInput(applicantInfo.FirstName, applicantInfo.LastName, applicantInfo.Phone, applicantInfo.Email, applicantInfo.CurrentAddress, applicantInfo.DesiredLeaseStartDate));

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Error!);
                var failVm = await BuildWizardViewModelAsync(applicationId, WizardSection.ApplicantInfo);
                if (failVm is null) return NotFound();
                failVm.ApplicantInfo = applicantInfo;
                Response.StatusCode = 400;
                return View(nameof(Wizard), failVm);
            }

            return RedirectToAction(nameof(Wizard), new { id = applicationId, section = WizardSection.ResidenceHistory });
        }

        if (currentSection == WizardSection.ResidenceHistory)
        {
            var result = await _applicantApplications.SaveResidenceHistoryAsync(applicationId, userId);
            if (!result.Succeeded)
            {
                TempData["Error"] = result.Error;
                return RedirectToAction(nameof(Wizard), new { id = applicationId, section = WizardSection.ResidenceHistory });
            }

            return RedirectToAction(nameof(Wizard), new { id = applicationId, section = WizardSection.Summary });
        }

        if (currentSection == WizardSection.Summary && action == "submit")
        {
            var result = await _applicantApplications.SubmitAsync(applicationId, userId);
            if (!result.Succeeded)
            {
                TempData["Error"] = result.Error;
                return RedirectToAction(nameof(Wizard), new { id = applicationId, section = WizardSection.Summary });
            }

            return RedirectToAction(nameof(Details), new { id = applicationId });
        }

        return RedirectToAction(nameof(Wizard), new { id = applicationId });
    }

    [Authorize(Policy = "RequireApplicant")]
    public async Task<IActionResult> ResidenceModal(int applicationId, int? id)
    {
        var userId = _userManager.GetUserId(User)!;
        var application = await _db.RentalApplications.Include(a => a.Residences)
            .FirstOrDefaultAsync(a => a.Id == applicationId && a.ApplicantId == userId);
        if (application is null) return NotFound();

        var model = new ResidenceFormViewModel
        {
            ApplicationId = applicationId,
            MoveInDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };
        if (id is { } residenceId)
        {
            var residence = application.Residences.FirstOrDefault(r => r.Id == residenceId);
            if (residence is null) return NotFound();

            model.Id = residence.Id;
            model.Address = residence.Address;
            model.LandlordName = residence.LandlordName;
            model.LandlordPhone = residence.LandlordPhone;
            model.MoveInDate = residence.MoveInDate;
            model.MoveOutDate = residence.MoveOutDate;
        }

        return PartialView("_ResidenceForm", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "RequireApplicant")]
    public async Task<IActionResult> SaveResidence(ResidenceFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            Response.StatusCode = 400;
            return PartialView("_ResidenceForm", model);
        }

        var userId = _userManager.GetUserId(User)!;
        var result = await _applicantApplications.AddOrUpdateResidenceAsync(model.ApplicationId, userId,
            new ResidenceInput(model.Id, model.Address, model.LandlordName, model.LandlordPhone, model.MoveInDate, model.MoveOutDate));

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            Response.StatusCode = 400;
            return PartialView("_ResidenceForm", model);
        }

        return Json(new { success = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "RequireApplicant")]
    public async Task<IActionResult> DeleteResidence(int applicationId, int residenceId)
    {
        var userId = _userManager.GetUserId(User)!;
        var result = await _applicantApplications.RemoveResidenceAsync(applicationId, userId, residenceId);
        if (!result.Succeeded)
        {
            return BadRequest(result.Error);
        }
        return Json(new { success = true });
    }

    [Authorize(Policy = "RequirePropertyManager")]
    public async Task<IActionResult> ReviewModal(int id)
    {
        var application = await _db.RentalApplications.FirstOrDefaultAsync(a => a.Id == id);
        if (application is null || application.Status != ApplicationStatus.Submitted) return NotFound();

        return PartialView("_ReviewForm", new ReviewFormViewModel { ApplicationId = id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "RequirePropertyManager")]
    public async Task<IActionResult> Review(ReviewFormViewModel model)
    {
        if (model.Outcome != ApplicationReviewOutcome.Approve && string.IsNullOrWhiteSpace(model.Comment))
        {
            ModelState.AddModelError(nameof(model.Comment), "A comment is required for this outcome.");
        }

        if (!ModelState.IsValid)
        {
            Response.StatusCode = 400;
            return PartialView("_ReviewForm", model);
        }

        var reviewerId = _userManager.GetUserId(User)!;
        var result = await _applicationReviews.ReviewAsync(model.ApplicationId, reviewerId, model.Outcome, model.Comment);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            Response.StatusCode = 400;
            return PartialView("_ReviewForm", model);
        }

        return Json(new { success = true });
    }
}
