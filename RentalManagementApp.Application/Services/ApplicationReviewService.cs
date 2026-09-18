using RentalManagementApp.Domain.Entities;
using RentalManagementApp.Domain.Enums;
using RentalManagementApp.Application.Contracts;
using RentalManagementApp.Application.Interfaces;

namespace RentalManagementApp.Application.Services;

public class ApplicationReviewService(
    IApplicationRepository applications,
    IUnitAvailabilityService availability,
    TimeProvider timeProvider) : IApplicationReviewService
{
    private DateTime Now => timeProvider.GetUtcNow().UtcDateTime;
    private DateOnly Today => DateOnly.FromDateTime(Now);

    public async Task<ServiceResult> ReviewAsync(
        int applicationId,
        string reviewerId,
        ApplicationReviewOutcome outcome,
        string? comment)
    {
        if (outcome is ApplicationReviewOutcome.Return or ApplicationReviewOutcome.Deny &&
            string.IsNullOrWhiteSpace(comment))
        {
            return ServiceResult.Failure("A comment is required when returning or denying an application.");
        }

        var application = await applications.GetApplicationForReviewAsync(applicationId);
        if (application is null)
        {
            return ServiceResult.Failure("Application not found.");
        }

        if (application.Unit!.Property!.PropertyManagerId != reviewerId)
        {
            return ServiceResult.Failure("You are not authorized to review applications for this property.");
        }

        if (application.Status != ApplicationStatus.Submitted)
        {
            return ServiceResult.Failure("Only submitted applications can be reviewed.");
        }

        switch (outcome)
        {
            case ApplicationReviewOutcome.Approve:
                var requestedStartDate = application.DesiredLeaseStartDate ?? Today;
                var startDate = requestedStartDate < Today ? Today : requestedStartDate;
                if (!availability.IsUnitAvailable(application.Unit!.Leases, startDate))
                {
                    return ServiceResult.Failure("This unit already has an active lease. Approval is blocked.");
                }

                application.Status = ApplicationStatus.Approved;
                applications.Add(new Lease
                {
                    UnitId = application.UnitId,
                    RentalApplicationId = application.Id,
                    StartDate = startDate,
                    EndDate = startDate.AddMonths(12).AddDays(-1),
                    MonthlyRent = application.Unit.MonthlyRent
                });
                break;
            case ApplicationReviewOutcome.Return:
                application.Status = ApplicationStatus.Returned;
                break;
            case ApplicationReviewOutcome.Deny:
                application.Status = ApplicationStatus.Denied;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null);
        }

        application.UpdatedAt = Now;
        application.StatusHistory.Add(new ApplicationStatusHistory
        {
            Status = application.Status,
            ChangedByUserId = reviewerId,
            ChangedAt = Now,
            Comment = comment
        });

        await applications.SaveChangesAsync();
        return ServiceResult.Success();
    }
}
