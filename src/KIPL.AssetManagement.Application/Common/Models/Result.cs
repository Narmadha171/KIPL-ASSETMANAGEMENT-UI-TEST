namespace KIPL.AssetManagement.Application.Common.Models;

/// <summary>Lightweight success/failure envelope so services never throw for expected failures.</summary>
public class Result
{
    protected Result(bool succeeded, string? error)
    {
        Succeeded = succeeded;
        Error = error;
    }

    public bool Succeeded { get; }
    public string? Error { get; }

    public static Result Success() => new(true, null);
    public static Result Failure(string error) => new(false, error);
}

public class Result<T> : Result
{
    private Result(bool succeeded, T? value, string? error) : base(succeeded, error) => Value = value;

    public T? Value { get; }

    public static Result<T> Success(T value) => new(true, value, null);
    public static new Result<T> Failure(string error) => new(false, default, error);
}
