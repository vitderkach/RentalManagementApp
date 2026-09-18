using RentalManagementApp.Services.Contracts;

namespace RentalManagementApp.Services.Interfaces;

public record ResidenceInput(
    int? Id,
    string Address,
    string LandlordName,
    string LandlordPhone,
    DateOnly MoveInDate,
    DateOnly? MoveOutDate);

public record ApplicantInfoInput(
    string FirstName,
    string LastName,
    string Phone,
    string Email,
    string CurrentAddress,
    DateOnly DesiredLeaseStartDate);

public interface IApplicantApplicationService
{
    Task<ServiceResult<int>> StartApplicationAsync(int unitId, string applicantId);

    Task<ServiceResult> SaveApplicantInfoAsync(int applicationId, string userId, ApplicantInfoInput input);

    Task<ServiceResult> SaveResidenceHistoryAsync(int applicationId, string userId);

    Task<ServiceResult<int>> AddOrUpdateResidenceAsync(int applicationId, string userId, ResidenceInput input);

    Task<ServiceResult> RemoveResidenceAsync(int applicationId, string userId, int residenceId);

    Task<ServiceResult> SubmitAsync(int applicationId, string userId);

    Task<ServiceResult> WithdrawAsync(int applicationId, string userId);
}

public interface IApplicationReviewService
{
    Task<ServiceResult> ReviewAsync(int applicationId, string reviewerId, ApplicationReviewOutcome outcome, string? comment);
}
