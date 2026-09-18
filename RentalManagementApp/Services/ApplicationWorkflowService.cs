using Microsoft.EntityFrameworkCore;
using RentalManagementApp.Data;
using RentalManagementApp.Data.Entities;
using RentalManagementApp.Data.Enums;
using RentalManagementApp.Services.Interfaces;

namespace RentalManagementApp.Services;

public class ApplicantApplicationService : IApplicantApplicationService
{
    private readonly ApplicationDbContext _db;
    private readonly IUnitAvailabilityService _availability;
    private readonly TimeProvider _timeProvider;

    public ApplicantApplicationService(
        ApplicationDbContext db,
        IUnitAvailabilityService availability,
        TimeProvider? timeProvider = null)
    {
        _db = db;
        _availability = availability;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    private DateTime Now => _timeProvider.GetUtcNow().UtcDateTime;
    private DateOnly Today => DateOnly.FromDateTime(Now);

    public async Task<ServiceResult<int>> StartApplicationAsync(int unitId, string applicantId)
    {
        var unit = await _db.Units.Include(u => u.Leases).FirstOrDefaultAsync(u => u.Id == unitId);
        if (unit is null)
        {
            return ServiceResult<int>.Failure("Unit not found.");
        }

        if (!_availability.IsUnitAvailable(unit.Leases, Today))
        {
            return ServiceResult<int>.Failure("This unit currently has an active lease and is not available.");
        }

        var application = new RentalApplication
        {
            UnitId = unitId,
            ApplicantId = applicantId,
            Status = ApplicationStatus.Draft,
            CreatedAt = Now,
            UpdatedAt = Now
        };
        application.StatusHistory.Add(new ApplicationStatusHistory
        {
            Status = ApplicationStatus.Draft,
            ChangedByUserId = applicantId,
            ChangedAt = Now,
            Comment = "Application created."
        });

        _db.RentalApplications.Add(application);
        await _db.SaveChangesAsync();
        return ServiceResult<int>.Success(application.Id);
    }

    private async Task<RentalApplication?> LoadOwnedEditableApplicationAsync(int applicationId, string userId)
    {
        var application = await _db.RentalApplications
            .Include(a => a.Residences)
            .Include(a => a.Unit)
            .FirstOrDefaultAsync(a => a.Id == applicationId);

        if (application is null || application.ApplicantId != userId)
        {
            return null;
        }

        return application;
    }

    public async Task<ServiceResult> SaveApplicantInfoAsync(int applicationId, string userId, ApplicantInfoInput input)
    {
        var application = await LoadOwnedEditableApplicationAsync(applicationId, userId);
        if (application is null)
        {
            return ServiceResult.Failure("Application not found.");
        }

        if (!application.Status.IsEditable())
        {
            return ServiceResult.Failure("This application can no longer be edited.");
        }

        application.ApplicantFirstName = input.FirstName;
        application.ApplicantLastName = input.LastName;
        application.ApplicantPhone = input.Phone;
        application.ApplicantEmail = input.Email;
        application.CurrentAddress = input.CurrentAddress;
        application.DesiredLeaseStartDate = input.DesiredLeaseStartDate;
        application.ApplicantInfoCompleted = true;
        application.UpdatedAt = Now;

        // Seed Residence History with the applicant's current address so they don't have
        // to re-enter it; they still need to fill in the landlord/move-in details.
        if (!application.Residences.Any(r => r.Address == input.CurrentAddress))
        {
            application.Residences.Add(new Residence
            {
                RentalApplicationId = application.Id,
                Address = input.CurrentAddress,
                MoveInDate = Today
            });
        }

        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> SaveResidenceHistoryAsync(int applicationId, string userId)
    {
        var application = await LoadOwnedEditableApplicationAsync(applicationId, userId);
        if (application is null)
        {
            return ServiceResult.Failure("Application not found.");
        }

        if (!application.Status.IsEditable())
        {
            return ServiceResult.Failure("This application can no longer be edited.");
        }

        application.ResidenceHistoryCompleted = true;
        application.UpdatedAt = Now;
        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult<int>> AddOrUpdateResidenceAsync(int applicationId, string userId, ResidenceInput input)
    {
        var application = await LoadOwnedEditableApplicationAsync(applicationId, userId);
        if (application is null)
        {
            return ServiceResult<int>.Failure("Application not found.");
        }

        if (!application.Status.IsEditable())
        {
            return ServiceResult<int>.Failure("This application can no longer be edited.");
        }

        if (input.MoveOutDate is { } moveOut && moveOut < input.MoveInDate)
        {
            return ServiceResult<int>.Failure("Move-out date cannot be earlier than the move-in date.");
        }

        Residence residence;
        if (input.Id is int id)
        {
            var existing = application.Residences.FirstOrDefault(r => r.Id == id);
            if (existing is null)
            {
                return ServiceResult<int>.Failure("Residence not found.");
            }
            residence = existing;
        }
        else
        {
            residence = new Residence { RentalApplicationId = application.Id };
            application.Residences.Add(residence);
        }

        residence.Address = input.Address;
        residence.LandlordName = input.LandlordName;
        residence.LandlordPhone = input.LandlordPhone;
        residence.MoveInDate = input.MoveInDate;
        residence.MoveOutDate = input.MoveOutDate;

        // Editing residences invalidates the previous section completion until Continue is pressed again.
        application.ResidenceHistoryCompleted = false;
        application.UpdatedAt = Now;

        await _db.SaveChangesAsync();
        return ServiceResult<int>.Success(residence.Id);
    }

    public async Task<ServiceResult> RemoveResidenceAsync(int applicationId, string userId, int residenceId)
    {
        var application = await LoadOwnedEditableApplicationAsync(applicationId, userId);
        if (application is null)
        {
            return ServiceResult.Failure("Application not found.");
        }

        if (!application.Status.IsEditable())
        {
            return ServiceResult.Failure("This application can no longer be edited.");
        }

        var residence = application.Residences.FirstOrDefault(r => r.Id == residenceId);
        if (residence is null)
        {
            return ServiceResult.Failure("Residence not found.");
        }

        application.Residences.Remove(residence);
        _db.Residences.Remove(residence);
        application.ResidenceHistoryCompleted = false;
        application.UpdatedAt = Now;

        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> SubmitAsync(int applicationId, string userId)
    {
        var application = await LoadOwnedEditableApplicationAsync(applicationId, userId);
        if (application is null)
        {
            return ServiceResult.Failure("Application not found.");
        }

        if (!application.Status.IsEditable())
        {
            return ServiceResult.Failure("This application can no longer be edited.");
        }

        if (!application.ApplicantInfoCompleted || !application.ResidenceHistoryCompleted)
        {
            return ServiceResult.Failure("Both sections must be completed before submitting.");
        }

        var unit = await _db.Units.Include(u => u.Leases).FirstAsync(u => u.Id == application.UnitId);
        if (!_availability.IsUnitAvailable(unit.Leases, Today))
        {
            return ServiceResult.Failure("This unit currently has an active lease and is no longer available.");
        }

        application.Status = ApplicationStatus.Submitted;
        application.UpdatedAt = Now;
        application.StatusHistory.Add(new ApplicationStatusHistory
        {
            Status = ApplicationStatus.Submitted,
            ChangedByUserId = userId,
            ChangedAt = Now,
            Comment = "Application submitted."
        });

        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> WithdrawAsync(int applicationId, string userId)
    {
        var application = await _db.RentalApplications.FirstOrDefaultAsync(a => a.Id == applicationId);
        if (application is null || application.ApplicantId != userId)
        {
            return ServiceResult.Failure("Application not found.");
        }

        if (application.Status.IsTerminal())
        {
            return ServiceResult.Failure("This application has already reached a final status.");
        }

        application.Status = ApplicationStatus.Withdrawn;
        application.UpdatedAt = Now;
        application.StatusHistory.Add(new ApplicationStatusHistory
        {
            Status = ApplicationStatus.Withdrawn,
            ChangedByUserId = userId,
            ChangedAt = Now,
            Comment = "Application withdrawn by applicant."
        });

        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }

}
