namespace RentalManagementApp.Data.Entities;

public enum ApplicationStatus
{
    Draft = 0,
    Submitted = 1,
    Returned = 2,
    Approved = 3,
    Denied = 4,
    Withdrawn = 5
}

/// <summary>
/// Statuses considered terminal - no further edits or transitions are permitted.
/// </summary>
public static class ApplicationStatusExtensions
{
    public static bool IsTerminal(this ApplicationStatus status) =>
        status is ApplicationStatus.Approved or ApplicationStatus.Denied or ApplicationStatus.Withdrawn;

    public static bool IsEditable(this ApplicationStatus status) =>
        status is ApplicationStatus.Draft or ApplicationStatus.Returned;
}
