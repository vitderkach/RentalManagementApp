using Microsoft.AspNetCore.Mvc;
using RentalManagementApp.Domain.Entities;
using RentalManagementApp.Domain.Enums;

namespace RentalManagementApp.ViewComponents;

public class StatusBadgeViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(ApplicationStatus status)
    {
        return View(status);
    }
}
