using Microsoft.EntityFrameworkCore;
using RentalManagementApp.Infrastructure.Data;
using RentalManagementApp.Domain.Entities;
using RentalManagementApp.Domain.Enums;
using RentalManagementApp.Application.Interfaces;
using RentalManagementApp.Infrastructure.Identity;

namespace RentalManagementApp.Infrastructure.Data.Repositories;

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
            .Include(a => a.StatusHistory)
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
            .Include(a => a.Unit).ThenInclude(u => u!.Leases)
            .FirstOrDefaultAsync(a => a.Id == applicationId);

    public async Task<Unit?> GetUnitWithLeasesAsync(int unitId) =>
        await db.Units
            .Include(u => u.Leases)
            .FirstOrDefaultAsync(u => u.Id == unitId);

    public void Add(RentalApplication application) => db.RentalApplications.Add(application);

    public void Add(Lease lease) => db.Leases.Add(lease);

    public void Remove(Residence residence) => db.Residences.Remove(residence);

    public Task SaveChangesAsync() => db.SaveChangesAsync();

    public async Task<IReadOnlyDictionary<string, string>> GetUserFullNamesAsync(IEnumerable<string> userIds) =>
        await db.Users
            .Where(user => userIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, user => user.FullName);
}
