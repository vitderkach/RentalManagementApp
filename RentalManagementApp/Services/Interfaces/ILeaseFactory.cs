using RentalManagementApp.Data.Entities;

namespace RentalManagementApp.Services.Interfaces;

public interface ILeaseFactory
{
    Lease CreateLeaseForApproval(RentalApplication application, DateOnly startDate);
}
