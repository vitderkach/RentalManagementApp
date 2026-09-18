namespace RentalManagementApp.Application.Services;

public class ServiceResult
{
    public bool Succeeded { get; }
    public string? Error { get; }

    protected ServiceResult(bool succeeded, string? error)
    {
        Succeeded = succeeded;
        Error = error;
    }

    public static ServiceResult Success() => new(true, null);
    public static ServiceResult Failure(string error) => new(false, error);
}

public class ServiceResult<T> : ServiceResult
{
    public T? Value { get; }

    private ServiceResult(bool succeeded, string? error, T? value) : base(succeeded, error)
    {
        Value = value;
    }

    public static ServiceResult<T> Success(T value) => new(true, null, value);
    public static new ServiceResult<T> Failure(string error) => new(false, error, default);
}
