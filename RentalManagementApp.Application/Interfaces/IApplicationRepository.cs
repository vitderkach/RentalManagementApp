using RentalManagementApp.Domain.Entities;
using RentalManagementApp.Domain.Enums;

namespace RentalManagementApp.Application.Interfaces;

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
    Task<Unit?> GetUnitWithLeasesAsync(int unitId);
    void Add(RentalApplication application);
    void Add(Lease lease);
    void Remove(Residence residence);
    Task SaveChangesAsync();
    Task<IReadOnlyDictionary<string, string>> GetUserFullNamesAsync(IEnumerable<string> userIds);
}
