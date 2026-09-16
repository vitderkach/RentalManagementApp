namespace RentalManagementApp.Data.Entities;

/// <summary>
/// A rental application submitted by an applicant for a single unit. Applicant Information and
/// Residence History are tracked as independently-completed sections so the wizard knows when
/// Submit may be enabled.
/// </summary>
public class RentalApplication
{
    public int Id { get; set; }

    public int UnitId { get; set; }
    public Unit? Unit { get; set; }

    public string ApplicantId { get; set; } = string.Empty;
    public ApplicationUser? Applicant { get; set; }

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Draft;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Applicant Information section
    public string ApplicantFirstName { get; set; } = string.Empty;
    public string ApplicantLastName { get; set; } = string.Empty;
    public string ApplicantPhone { get; set; } = string.Empty;
    public string ApplicantEmail { get; set; } = string.Empty;
    public string CurrentAddress { get; set; } = string.Empty;
    public bool ApplicantInfoCompleted { get; set; }

    // Residence History section
    public bool ResidenceHistoryCompleted { get; set; }

    public ICollection<Residence> Residences { get; set; } = new List<Residence>();
    public ICollection<ApplicationStatusHistory> StatusHistory { get; set; } = new List<ApplicationStatusHistory>();
    public Lease? Lease { get; set; }

    /// <summary>Concurrency token guarding against lost updates when a section is saved.</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
