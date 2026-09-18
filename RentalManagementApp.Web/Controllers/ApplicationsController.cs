using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Json;
using RentalManagementApp.Domain.Entities;
using RentalManagementApp.Domain.Enums;
using RentalManagementApp.Infrastructure.Identity;
using RentalManagementApp.Application.Contracts;
using RentalManagementApp.Application.Interfaces;
using RentalManagementApp.ViewModels.Applications;
using RentalManagementApp.ViewModels.Applications.Enums;

namespace RentalManagementApp.Controllers;

[Authorize]
public class ApplicationsController : Controller
{
    private const string PendingResidencesSessionKeyPrefix = "PendingResidences-";
    private readonly IApplicationRepository _applicationRepository;
    private readonly IApplicantApplicationService _applicantApplications;
    private readonly IApplicationReviewService _applicationReviews;
    private readonly UserManager<ApplicationUser> _userManager;

    public ApplicationsController(
        IApplicationRepository applicationRepository,
        IApplicantApplicationService applicantApplications,
        IApplicationReviewService applicationReviews,
        UserManager<ApplicationUser> userManager)
    {
        _applicationRepository = applicationRepository;
        _applicantApplications = applicantApplications;
        _applicationReviews = applicationReviews;
        _userManager = userManager;
    }

    private bool IsManager => User.IsInRole(Roles.PropertyManager);

    public async Task<IActionResult> Index(ApplicationStatus? status, int? propertyId)
    {
        var userId = _userManager.GetUserId(User)!;
        var applications = await _applicationRepository.GetApplicationsAsync(userId, IsManager, status, propertyId);
        var items = applications
            .Select(a => new ApplicationListItemViewModel
            {
                Id = a.Id,
                PropertyName = a.Unit!.Property!.Name,
                UnitNumber = a.Unit.UnitNumber,
                ApplicantName = a.ApplicantFirstName + " " + a.ApplicantLastName,
                Status = a.Status,
                UpdatedAt = a.UpdatedAt
            })
            .ToList();

        var properties = await _applicationRepository.GetManagedPropertiesAsync(userId);
        var propertyOptions = properties
            .Select(p => new SelectListItem(p.Name, p.Id.ToString(), p.Id == propertyId))
            .ToList();

        var vm = new ApplicationListViewModel
        {
            IsManager = IsManager,
            Items = items,
            StatusFilter = status,
            PropertyFilter = propertyId,
            StatusOptions = Enum.GetValues<ApplicationStatus>()
                .Select(s => new SelectListItem(s.ToString(), ((int)s).ToString(), s == status)),
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
        var application = await _applicationRepository.GetApplicationDetailsAsync(id);

        if (application is null) return NotFound();
        if (IsManager && application.Unit!.Property!.PropertyManagerId != userId) return Forbid();
        if (!IsManager && application.ApplicantId != userId) return Forbid();
        var userNames = await _applicationRepository.GetUserFullNamesAsync(
            application.StatusHistory.Select(h => h.ChangedByUserId));

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
                    ChangedByName = userNames.GetValueOrDefault(h.ChangedByUserId, "System"),
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

    private string PendingResidencesSessionKey(int applicationId) =>
        $"{PendingResidencesSessionKeyPrefix}{applicationId}";

    private List<ResidenceFormViewModel> GetPendingResidences(int applicationId) =>
        JsonSerializer.Deserialize<List<ResidenceFormViewModel>>(
            HttpContext.Session.GetString(PendingResidencesSessionKey(applicationId)) ?? "[]") ?? [];

    private void SetPendingResidences(int applicationId, List<ResidenceFormViewModel> residences) =>
        HttpContext.Session.SetString(
            PendingResidencesSessionKey(applicationId),
            JsonSerializer.Serialize(residences));

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
        var application = await _applicationRepository.GetApplicantApplicationForWizardAsync(id, userId);

        if (application is null) return null;

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
            Residences = application.Residences.Select(MapResidence)
                .Concat(GetPendingResidences(id).Select((r, index) => new ResidenceViewModel
                {
                    IsPending = true,
                    PendingIndex = index,
                    Address = r.Address,
                    LandlordName = r.LandlordName,
                    LandlordPhone = r.LandlordPhone,
                    MoveInDate = r.MoveInDate,
                    MoveOutDate = r.MoveOutDate
                }))
                .ToList()
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
            if (currentSection == WizardSection.ResidenceHistory)
            {
                HttpContext.Session.Remove(PendingResidencesSessionKey(applicationId));
            }

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
            var pendingResidences = GetPendingResidences(applicationId);
            foreach (var residence in pendingResidences)
            {
                var saveResidenceResult = await _applicantApplications.AddOrUpdateResidenceAsync(
                    applicationId,
                    userId,
                    new ResidenceInput(
                        null,
                        residence.Address,
                        residence.LandlordName,
                        residence.LandlordPhone,
                        residence.MoveInDate,
                        residence.MoveOutDate));
                if (!saveResidenceResult.Succeeded)
                {
                    TempData["Error"] = saveResidenceResult.Error;
                    return RedirectToAction(nameof(Wizard), new { id = applicationId, section = WizardSection.ResidenceHistory });
                }
            }

            var result = await _applicantApplications.SaveResidenceHistoryAsync(applicationId, userId);
            if (!result.Succeeded)
            {
                TempData["Error"] = result.Error;
                return RedirectToAction(nameof(Wizard), new { id = applicationId, section = WizardSection.ResidenceHistory });
            }

            HttpContext.Session.Remove(PendingResidencesSessionKey(applicationId));
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
    public async Task<IActionResult> ResidenceModal(int applicationId, int? id, int? pendingIndex)
    {
        var userId = _userManager.GetUserId(User)!;
        var application = await _applicationRepository
            .GetApplicantApplicationWithResidencesAsync(applicationId, userId);
        if (application is null) return NotFound();

        var model = new ResidenceFormViewModel
        {
            ApplicationId = applicationId,
            MoveInDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };
        if (pendingIndex is { } index)
        {
            var pendingResidences = GetPendingResidences(applicationId);
            if (index < 0 || index >= pendingResidences.Count) return NotFound();

            model = pendingResidences[index];
            model.PendingIndex = index;
        }
        else if (id is { } residenceId)
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
        if (model.Id is null)
        {
            var application = await _applicationRepository
                .GetApplicantApplicationAsync(model.ApplicationId, userId);
            if (application is null || !application.Status.IsEditable())
            {
                ModelState.AddModelError(string.Empty, "This application can no longer be edited.");
                Response.StatusCode = 400;
                return PartialView("_ResidenceForm", model);
            }

            var pendingResidences = GetPendingResidences(model.ApplicationId);
            if (model.PendingIndex is { } index)
            {
                if (index < 0 || index >= pendingResidences.Count)
                {
                    return NotFound();
                }

                pendingResidences[index] = model;
            }
            else
            {
                pendingResidences.Add(model);
            }

            SetPendingResidences(model.ApplicationId, pendingResidences);
            return Json(new { success = true });
        }

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
    public async Task<IActionResult> DeleteResidence(int applicationId, int? residenceId, int? pendingIndex)
    {
        var userId = _userManager.GetUserId(User)!;
        if (pendingIndex is { } index)
        {
            var application = await _applicationRepository
                .GetApplicantApplicationAsync(applicationId, userId);
            if (application is null || !application.Status.IsEditable()) return BadRequest("This application can no longer be edited.");

            var pendingResidences = GetPendingResidences(applicationId);
            if (index < 0 || index >= pendingResidences.Count) return NotFound();

            pendingResidences.RemoveAt(index);
            SetPendingResidences(applicationId, pendingResidences);
            return Json(new { success = true });
        }

        if (residenceId is null) return BadRequest("Residence not found.");

        var result = await _applicantApplications.RemoveResidenceAsync(applicationId, userId, residenceId.Value);
        if (!result.Succeeded)
        {
            return BadRequest(result.Error);
        }
        return Json(new { success = true });
    }

    [Authorize(Policy = "RequirePropertyManager")]
    public async Task<IActionResult> ReviewModal(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var application = await _applicationRepository.GetApplicationForReviewAsync(id);
        if (application is null || application.Status != ApplicationStatus.Submitted) return NotFound();
        if (application.Unit!.Property!.PropertyManagerId != userId) return Forbid();

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
