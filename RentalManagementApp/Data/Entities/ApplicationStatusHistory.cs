namespace RentalManagementApp.Data.Entities;

/// <summary>
/// Audit trail entry recording every status transition (submission, review outcome, withdrawal)
/// so the application page can display who changed what, and when.
/// </summary>
public class ApplicationStatusHistory
{
    public int Id { get; set; }

    public int RentalApplicationId { get; set; }
    public RentalApplication? RentalApplication { get; set; }

    public ApplicationStatus Status { get; set; }
    public string ChangedByUserId { get; set; } = string.Empty;
    public ApplicationUser? ChangedByUser { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? Comment { get; set; }
}
