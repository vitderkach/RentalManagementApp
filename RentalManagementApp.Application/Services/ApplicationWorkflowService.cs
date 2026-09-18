using RentalManagementApp.Domain.Entities;
using RentalManagementApp.Domain.Enums;
using RentalManagementApp.Application.Interfaces;

namespace RentalManagementApp.Application.Services;

public class ApplicantApplicationService(
    IApplicationRepository applications,
    IUnitAvailabilityService availability,
    TimeProvider? timeProvider = null) : IApplicantApplicationService
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private DateTime Now => _timeProvider.GetUtcNow().UtcDateTime;
    private DateOnly Today => DateOnly.FromDateTime(Now);

    public async Task<ServiceResult<int>> StartApplicationAsync(int unitId, string applicantId)
    {
        var unit = await applications.GetUnitWithLeasesAsync(unitId);
        if (unit is null) return ServiceResult<int>.Failure("Unit not found.");
        if (!availability.IsUnitAvailable(unit.Leases, Today))
            return ServiceResult<int>.Failure("This unit currently has an active lease and is not available.");

        var application = new RentalApplication
        {
            UnitId = unitId, ApplicantId = applicantId, Status = ApplicationStatus.Draft,
            CreatedAt = Now, UpdatedAt = Now
        };
        application.StatusHistory.Add(new ApplicationStatusHistory
        {
            Status = ApplicationStatus.Draft, ChangedByUserId = applicantId,
            ChangedAt = Now, Comment = "Application created."
        });
        applications.Add(application);
        await applications.SaveChangesAsync();
        return ServiceResult<int>.Success(application.Id);
    }

    private Task<RentalApplication?> LoadOwnedEditableApplicationAsync(int applicationId, string userId) =>
        applications.GetApplicantApplicationForWizardAsync(applicationId, userId);

    public async Task<ServiceResult> SaveApplicantInfoAsync(int applicationId, string userId, ApplicantInfoInput input)
    {
        var application = await LoadOwnedEditableApplicationAsync(applicationId, userId);
        if (application is null) return ServiceResult.Failure("Application not found.");
        if (!application.Status.IsEditable()) return ServiceResult.Failure("This application can no longer be edited.");

        application.ApplicantFirstName = input.FirstName;
        application.ApplicantLastName = input.LastName;
        application.ApplicantPhone = input.Phone;
        application.ApplicantEmail = input.Email;
        application.CurrentAddress = input.CurrentAddress;
        application.DesiredLeaseStartDate = input.DesiredLeaseStartDate;
        application.ApplicantInfoCompleted = true;
        application.UpdatedAt = Now;
        if (!application.Residences.Any(r => r.Address == input.CurrentAddress))
            application.Residences.Add(new Residence { RentalApplicationId = application.Id, Address = input.CurrentAddress, MoveInDate = Today });

        await applications.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> SaveResidenceHistoryAsync(int applicationId, string userId)
    {
        var application = await LoadOwnedEditableApplicationAsync(applicationId, userId);
        if (application is null) return ServiceResult.Failure("Application not found.");
        if (!application.Status.IsEditable()) return ServiceResult.Failure("This application can no longer be edited.");
        application.ResidenceHistoryCompleted = true;
        application.UpdatedAt = Now;
        await applications.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult<int>> AddOrUpdateResidenceAsync(int applicationId, string userId, ResidenceInput input)
    {
        var application = await LoadOwnedEditableApplicationAsync(applicationId, userId);
        if (application is null) return ServiceResult<int>.Failure("Application not found.");
        if (!application.Status.IsEditable()) return ServiceResult<int>.Failure("This application can no longer be edited.");
        if (input.MoveOutDate is { } moveOut && moveOut < input.MoveInDate)
            return ServiceResult<int>.Failure("Move-out date cannot be earlier than the move-in date.");

        var residence = input.Id is int id
            ? application.Residences.FirstOrDefault(r => r.Id == id)
            : new Residence { RentalApplicationId = application.Id };
        if (residence is null) return ServiceResult<int>.Failure("Residence not found.");
        if (input.Id is null) application.Residences.Add(residence);

        residence.Address = input.Address;
        residence.LandlordName = input.LandlordName;
        residence.LandlordPhone = input.LandlordPhone;
        residence.MoveInDate = input.MoveInDate;
        residence.MoveOutDate = input.MoveOutDate;
        application.ResidenceHistoryCompleted = false;
        application.UpdatedAt = Now;
        await applications.SaveChangesAsync();
        return ServiceResult<int>.Success(residence.Id);
    }

    public async Task<ServiceResult> RemoveResidenceAsync(int applicationId, string userId, int residenceId)
    {
        var application = await LoadOwnedEditableApplicationAsync(applicationId, userId);
        if (application is null) return ServiceResult.Failure("Application not found.");
        if (!application.Status.IsEditable()) return ServiceResult.Failure("This application can no longer be edited.");
        var residence = application.Residences.FirstOrDefault(r => r.Id == residenceId);
        if (residence is null) return ServiceResult.Failure("Residence not found.");
        application.Residences.Remove(residence);
        applications.Remove(residence);
        application.ResidenceHistoryCompleted = false;
        application.UpdatedAt = Now;
        await applications.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> SubmitAsync(int applicationId, string userId)
    {
        var application = await LoadOwnedEditableApplicationAsync(applicationId, userId);
        if (application is null) return ServiceResult.Failure("Application not found.");
        if (!application.Status.IsEditable()) return ServiceResult.Failure("This application can no longer be edited.");
        if (!application.ApplicantInfoCompleted || !application.ResidenceHistoryCompleted)
            return ServiceResult.Failure("Both sections must be completed before submitting.");

        var unit = await applications.GetUnitWithLeasesAsync(application.UnitId);
        if (unit is null || !availability.IsUnitAvailable(unit.Leases, Today))
            return ServiceResult.Failure("This unit currently has an active lease and is no longer available.");

        application.Status = ApplicationStatus.Submitted;
        application.UpdatedAt = Now;
        application.StatusHistory.Add(new ApplicationStatusHistory
        {
            Status = ApplicationStatus.Submitted, ChangedByUserId = userId,
            ChangedAt = Now, Comment = "Application submitted."
        });
        await applications.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> WithdrawAsync(int applicationId, string userId)
    {
        var application = await applications.GetApplicantApplicationAsync(applicationId, userId);
        if (application is null) return ServiceResult.Failure("Application not found.");
        if (application.Status.IsTerminal()) return ServiceResult.Failure("This application has already reached a final status.");
        application.Status = ApplicationStatus.Withdrawn;
        application.UpdatedAt = Now;
        application.StatusHistory.Add(new ApplicationStatusHistory
        {
            Status = ApplicationStatus.Withdrawn, ChangedByUserId = userId,
            ChangedAt = Now, Comment = "Application withdrawn by applicant."
        });
        await applications.SaveChangesAsync();
        return ServiceResult.Success();
    }
}
