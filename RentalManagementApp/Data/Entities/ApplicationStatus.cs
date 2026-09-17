using RentalManagementApp.Data.Enums;

namespace RentalManagementApp.Data.Entities;

public static class ApplicationStatusExtensions
{
    public static bool IsTerminal(this ApplicationStatus status) =>
        status is ApplicationStatus.Approved or ApplicationStatus.Denied or ApplicationStatus.Withdrawn;

    public static bool IsEditable(this ApplicationStatus status) =>
        status is ApplicationStatus.Draft or ApplicationStatus.Returned;
}
