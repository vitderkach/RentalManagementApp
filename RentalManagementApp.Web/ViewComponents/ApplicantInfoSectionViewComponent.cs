using Microsoft.AspNetCore.Mvc;
using RentalManagementApp.ViewModels.Applications;

namespace RentalManagementApp.ViewComponents;

public class ApplicantInfoSectionViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(ApplicationWizardViewModel model)
    {
        return View(model);
    }
}
