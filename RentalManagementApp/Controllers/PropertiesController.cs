using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RentalManagementApp.Data;
using RentalManagementApp.Data.Entities;
using RentalManagementApp.Services;
using RentalManagementApp.Services.Interfaces;
using RentalManagementApp.ViewModels.Properties;

namespace RentalManagementApp.Controllers;

[Authorize]
public class PropertiesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IPropertyManagementService _propertyService;
    private readonly UserManager<ApplicationUser> _userManager;

    public PropertiesController(
        ApplicationDbContext db, IPropertyManagementService propertyService, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _propertyService = propertyService;
        _userManager = userManager;
    }

    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    [Authorize(Policy = "RequirePropertyManager")]
    public async Task<IActionResult> Index()
    {
        var managerId = _userManager.GetUserId(User)!;
        var properties = await _db.Properties
            .Where(p => p.PropertyManagerId == managerId)
            .Include(p => p.Units).ThenInclude(u => u.UnitType)
            .Include(p => p.Units).ThenInclude(u => u.Leases)
            .Include(p => p.Units).ThenInclude(u => u.RentalApplications)
            .OrderBy(p => p.Name)
            .ToListAsync();

        var vm = new PropertiesIndexViewModel
        {
            Properties = properties.Select(MapProperty).ToList()
        };

        return View(vm);
    }

    private static PropertyDetailViewModel MapProperty(Property p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        AddressLine1 = p.AddressLine1,
        AddressLine2 = p.AddressLine2,
        City = p.City,
        State = p.State,
        ZipCode = p.ZipCode,
        Units = p.Units.OrderBy(u => u.UnitNumber).Select(u => new UnitRowViewModel
        {
            Id = u.Id,
            PropertyId = u.PropertyId,
            UnitNumber = u.UnitNumber,
            Bedrooms = u.Bedrooms,
            MonthlyRent = u.MonthlyRent,
            UnitTypeName = u.UnitType!.Name,
            IsAvailable = u.Leases.All(l => !l.CoversDate(Today)),
            HasApplications = u.RentalApplications.Count != 0
        }).ToList()
    };

    [Authorize(Policy = "RequireApplicant")]
    public async Task<IActionResult> Browse()
    {
        var userId = _userManager.GetUserId(User)!;
        var units = await _db.Units
            .Include(u => u.Property)
            .Include(u => u.UnitType)
            .Include(u => u.Leases)
            .Include(u => u.RentalApplications)
            .ToListAsync();

        var available = units
            .Where(u => u.Leases.All(l => !l.CoversDate(Today)))
            .OrderBy(u => u.Property!.Name).ThenBy(u => u.UnitNumber)
            .Select(u => new AvailableUnitViewModel
            {
                UnitId = u.Id,
                PropertyName = u.Property!.Name,
                Address = $"{u.Property.AddressLine1}, {u.Property.City}, {u.Property.State}",
                UnitNumber = u.UnitNumber,
                Bedrooms = u.Bedrooms,
                MonthlyRent = u.MonthlyRent,
                UnitTypeName = u.UnitType!.Name,
                HasOpenApplicationForCurrentUser = u.RentalApplications.Any(a =>
                    a.ApplicantId == userId && !a.Status.IsTerminal())
            })
            .ToList();

        return View(available);
    }

    // ----- Property modal -----

    [Authorize(Policy = "RequirePropertyManager")]
    public async Task<IActionResult> PropertyModal(int? id)
    {
        if (id is null)
        {
            return PartialView("_PropertyForm", new PropertyFormViewModel());
        }

        var managerId = _userManager.GetUserId(User)!;
        var property = await _db.Properties.FirstOrDefaultAsync(p => p.Id == id && p.PropertyManagerId == managerId);
        if (property is null) return NotFound();

        return PartialView("_PropertyForm", new PropertyFormViewModel
        {
            Id = property.Id,
            Name = property.Name,
            AddressLine1 = property.AddressLine1,
            AddressLine2 = property.AddressLine2,
            City = property.City,
            State = property.State,
            ZipCode = property.ZipCode
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "RequirePropertyManager")]
    public async Task<IActionResult> SaveProperty(PropertyFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            Response.StatusCode = 400;
            return PartialView("_PropertyForm", model);
        }

        var managerId = _userManager.GetUserId(User)!;
        ServiceResult result;
        if (model.Id is int id)
        {
            result = await _propertyService.UpdatePropertyAsync(id, managerId, model.Name, model.AddressLine1, model.AddressLine2, model.City, model.State, model.ZipCode);
        }
        else
        {
            result = await _propertyService.CreatePropertyAsync(managerId, model.Name, model.AddressLine1, model.AddressLine2, model.City, model.State, model.ZipCode);
        }

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            Response.StatusCode = 400;
            return PartialView("_PropertyForm", model);
        }

        return Json(new { success = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "RequirePropertyManager")]
    public async Task<IActionResult> DeleteProperty(int id)
    {
        var managerId = _userManager.GetUserId(User)!;
        var result = await _propertyService.DeletePropertyAsync(id, managerId);
        if (!result.Succeeded)
        {
            return BadRequest(result.Error);
        }
        return Json(new { success = true });
    }

    // ----- Unit modal -----

    [Authorize(Policy = "RequirePropertyManager")]
    public async Task<IActionResult> UnitModal(int propertyId, int? id)
    {
        var managerId = _userManager.GetUserId(User)!;
        var property = await _db.Properties.FirstOrDefaultAsync(p => p.Id == propertyId && p.PropertyManagerId == managerId);
        if (property is null) return NotFound();

        var model = new UnitFormViewModel { PropertyId = propertyId };

        if (id is int unitId)
        {
            var unit = await _db.Units.FirstOrDefaultAsync(u => u.Id == unitId && u.PropertyId == propertyId);
            if (unit is null) return NotFound();

            model.Id = unit.Id;
            model.UnitNumber = unit.UnitNumber;
            model.Bedrooms = unit.Bedrooms;
            model.MonthlyRent = unit.MonthlyRent;
            model.UnitTypeId = unit.UnitTypeId;
        }

        model.UnitTypeOptions = await GetUnitTypeOptionsAsync(model.UnitTypeId);
        return PartialView("_UnitForm", model);
    }

    private async Task<List<SelectListItem>> GetUnitTypeOptionsAsync(int currentlySelectedId)
    {
        var types = await _db.UnitTypes
            .Where(t => t.IsActive || t.Id == currentlySelectedId)
            .OrderBy(t => t.Name)
            .ToListAsync();

        return types.Select(t => new SelectListItem(
            t.IsActive ? t.Name : $"{t.Name} (inactive)", t.Id.ToString())).ToList();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "RequirePropertyManager")]
    public async Task<IActionResult> SaveUnit(UnitFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.UnitTypeOptions = await GetUnitTypeOptionsAsync(model.UnitTypeId);
            Response.StatusCode = 400;
            return PartialView("_UnitForm", model);
        }

        var managerId = _userManager.GetUserId(User)!;
        var result = await _propertyService.SaveUnitAsync(
            model.PropertyId, managerId,
            new UnitInput(model.Id, model.UnitNumber, model.Bedrooms, model.MonthlyRent, model.UnitTypeId));

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            model.UnitTypeOptions = await GetUnitTypeOptionsAsync(model.UnitTypeId);
            Response.StatusCode = 400;
            return PartialView("_UnitForm", model);
        }

        return Json(new { success = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "RequirePropertyManager")]
    public async Task<IActionResult> DeleteUnit(int id)
    {
        var managerId = _userManager.GetUserId(User)!;
        var result = await _propertyService.DeleteUnitAsync(id, managerId);
        if (!result.Succeeded)
        {
            return BadRequest(result.Error);
        }
        return Json(new { success = true });
    }
}
