using RentalManagementApp.Data.Entities;
using RentalManagementApp.Data.Enums;

namespace RentalManagementApp.Services.Interfaces;

public interface IApplicationRepository
{
    Task<IReadOnlyList<RentalApplication>> GetApplicationsAsync(
        string userId,
        bool isManager,
        ApplicationStatus? status,
        int? propertyId);

    Task<IReadOnlyList<Property>> GetManagedPropertiesAsync(string managerId);
    Task<RentalApplication?> GetApplicationDetailsAsync(int applicationId);
    Task<RentalApplication?> GetApplicantApplicationWithResidencesAsync(int applicationId, string applicantId);
    Task<RentalApplication?> GetApplicantApplicationForWizardAsync(int applicationId, string applicantId);
    Task<RentalApplication?> GetApplicantApplicationAsync(int applicationId, string applicantId);
    Task<RentalApplication?> GetApplicationForReviewAsync(int applicationId);
}
