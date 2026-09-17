using Microsoft.AspNetCore.Mvc;
using RentalManagementApp.Data.Entities;
using RentalManagementApp.Data.Enums;

namespace RentalManagementApp.ViewComponents;

public class StatusBadgeViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(ApplicationStatus status)
    {
        return View(status);
    }
}
