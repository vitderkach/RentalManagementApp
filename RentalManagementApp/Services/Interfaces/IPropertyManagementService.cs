namespace RentalManagementApp.Services.Interfaces;

public record UnitInput(int? Id, string UnitNumber, int Bedrooms, decimal MonthlyRent, int UnitTypeId);

public interface IPropertyManagementService
{
    Task<ServiceResult<int>> CreatePropertyAsync(string managerId, string name, string addressLine1, string? addressLine2, string city, string state, string zip);
    Task<ServiceResult> UpdatePropertyAsync(int propertyId, string managerId, string name, string addressLine1, string? addressLine2, string city, string state, string zip);
    Task<ServiceResult> DeletePropertyAsync(int propertyId, string managerId);

    Task<ServiceResult<int>> SaveUnitAsync(int propertyId, string managerId, UnitInput input);
    Task<ServiceResult> DeleteUnitAsync(int unitId, string managerId);
}
