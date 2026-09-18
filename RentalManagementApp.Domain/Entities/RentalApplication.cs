using RentalManagementApp.Domain.Enums;

namespace RentalManagementApp.Domain.Entities;

public class RentalApplication
{
    public int Id { get; set; }

    public int UnitId { get; set; }
    public Unit? Unit { get; set; }

    public string ApplicantId { get; set; } = string.Empty;

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Draft;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    public string ApplicantFirstName { get; set; } = string.Empty;
    public string ApplicantLastName { get; set; } = string.Empty;
    public string ApplicantPhone { get; set; } = string.Empty;
    public string ApplicantEmail { get; set; } = string.Empty;
    public string CurrentAddress { get; set; } = string.Empty;
    public DateOnly? DesiredLeaseStartDate { get; set; }
    public bool ApplicantInfoCompleted { get; set; }
    
    public bool ResidenceHistoryCompleted { get; set; }

    public ICollection<Residence> Residences { get; set; } = new List<Residence>();
    public ICollection<ApplicationStatusHistory> StatusHistory { get; set; } = new List<ApplicationStatusHistory>();
    public Lease? Lease { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
