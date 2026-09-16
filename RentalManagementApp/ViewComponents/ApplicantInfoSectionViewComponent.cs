using Microsoft.AspNetCore.Mvc;
using RentalManagementApp.ViewModels.Applications;

namespace RentalManagementApp.ViewComponents;

/// <summary>Renders the Applicant Information section of the application wizard, editable or read-only.</summary>
public class ApplicantInfoSectionViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(ApplicationWizardViewModel model)
    {
        return View(model);
    }
}
