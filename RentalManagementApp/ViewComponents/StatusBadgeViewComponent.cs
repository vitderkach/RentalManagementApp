using Microsoft.AspNetCore.Mvc;
using RentalManagementApp.Data.Entities;

namespace RentalManagementApp.ViewComponents;

/// <summary>Renders a colored Bootstrap badge for an application status. Reused across list/detail views.</summary>
public class StatusBadgeViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(ApplicationStatus status)
    {
        return View(status);
    }
}
