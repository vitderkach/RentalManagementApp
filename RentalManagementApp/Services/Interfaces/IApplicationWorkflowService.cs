namespace RentalManagementApp.Services.Interfaces;

public enum ReviewOutcome
{
    Approve,
    Return,
    Deny
}

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
    string CurrentAddress);

/// <summary>
/// Orchestrates the rental application lifecycle: creation, section saves, submission,
/// withdrawal, and property-manager review (approve/return/deny), including lease issuance.
/// </summary>
public interface IApplicationWorkflowService
{
    Task<ServiceResult<int>> StartApplicationAsync(int unitId, string applicantId);

    Task<ServiceResult> SaveApplicantInfoAsync(int applicationId, string userId, ApplicantInfoInput input);

    Task<ServiceResult> SaveResidenceHistoryAsync(int applicationId, string userId);

    Task<ServiceResult<int>> AddOrUpdateResidenceAsync(int applicationId, string userId, ResidenceInput input);

    Task<ServiceResult> RemoveResidenceAsync(int applicationId, string userId, int residenceId);

    Task<ServiceResult> SubmitAsync(int applicationId, string userId);

    Task<ServiceResult> WithdrawAsync(int applicationId, string userId);

    Task<ServiceResult> ReviewAsync(int applicationId, string reviewerId, ReviewOutcome outcome, string? comment);
}
