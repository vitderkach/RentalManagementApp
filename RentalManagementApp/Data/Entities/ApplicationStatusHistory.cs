using RentalManagementApp.Data.Enums;

namespace RentalManagementApp.Data.Entities;

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
