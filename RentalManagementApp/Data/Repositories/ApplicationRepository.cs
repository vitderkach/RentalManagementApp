using Microsoft.EntityFrameworkCore;
using RentalManagementApp.Data;
using RentalManagementApp.Data.Entities;
using RentalManagementApp.Data.Enums;
using RentalManagementApp.Services.Interfaces;

namespace RentalManagementApp.Data.Repositories;

public class ApplicationRepository(ApplicationDbContext db) : IApplicationRepository
{
    public async Task<IReadOnlyList<RentalApplication>> GetApplicationsAsync(
        string userId,
        bool isManager,
        ApplicationStatus? status,
        int? propertyId)
    {
        var query = db.RentalApplications
            .Include(a => a.Unit).ThenInclude(u => u!.Property)
            .Include(a => a.Applicant)
            .AsQueryable();

        query = isManager
            ? query.Where(a => a.Unit!.Property!.PropertyManagerId == userId)
            : query.Where(a => a.ApplicantId == userId);

        if (status is not null)
        {
            query = query.Where(a => a.Status == status);
        }

        if (propertyId is not null)
        {
            query = query.Where(a => a.Unit!.PropertyId == propertyId);
        }

        return await query.OrderByDescending(a => a.UpdatedAt).ToListAsync();
    }

    public async Task<IReadOnlyList<Property>> GetManagedPropertiesAsync(string managerId) =>
        await db.Properties
            .Where(p => p.PropertyManagerId == managerId)
            .OrderBy(p => p.Name)
            .ToListAsync();

    public async Task<RentalApplication?> GetApplicationDetailsAsync(int applicationId) =>
        await db.RentalApplications
            .Include(a => a.Unit).ThenInclude(u => u!.Property)
            .Include(a => a.Residences)
            .Include(a => a.StatusHistory).ThenInclude(h => h.ChangedByUser)
            .FirstOrDefaultAsync(a => a.Id == applicationId);

    public async Task<RentalApplication?> GetApplicantApplicationWithResidencesAsync(int applicationId, string applicantId) =>
        await db.RentalApplications
            .Include(a => a.Residences)
            .FirstOrDefaultAsync(a => a.Id == applicationId && a.ApplicantId == applicantId);

    public async Task<RentalApplication?> GetApplicantApplicationForWizardAsync(int applicationId, string applicantId) =>
        await db.RentalApplications
            .Include(a => a.Unit).ThenInclude(u => u!.Property)
            .Include(a => a.Residences)
            .FirstOrDefaultAsync(a => a.Id == applicationId && a.ApplicantId == applicantId);

    public async Task<RentalApplication?> GetApplicantApplicationAsync(int applicationId, string applicantId) =>
        await db.RentalApplications
            .FirstOrDefaultAsync(a => a.Id == applicationId && a.ApplicantId == applicantId);

    public async Task<RentalApplication?> GetApplicationForReviewAsync(int applicationId) =>
        await db.RentalApplications
            .Include(a => a.Unit).ThenInclude(u => u!.Property)
            .FirstOrDefaultAsync(a => a.Id == applicationId);
}
