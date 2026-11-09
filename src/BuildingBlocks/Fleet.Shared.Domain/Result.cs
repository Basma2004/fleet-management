namespace Fleet.Shared.Domain;

/// <summary>
/// Represents the outcome of an operation without throwing exceptions for predictable domain failures.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }

    protected Result(bool isSuccess, string? error)
    {
        if (isSuccess && error is not null)
            throw new InvalidOperationException("Successful result cannot have an error.");
        if (!isSuccess && error is null)
            throw new InvalidOperationException("Failed result must have an error message.");

        IsSuccess = isSuccess;
        Error = error;
    }

    // Non-generic results
    public static Result Success() => new(true, null);
    public static Result Failure(string error) => new(false, error);

    // Generic results (defined on non-generic class to allow C# type inference and satisfy CA1000)
    public static Result<T> Success<T>(T value) => new(true, null, value);
    public static Result<T> Failure<T>(string error) => new(false, error, default);
}

/// <summary>
/// Represents the outcome of an operation that returns a value on success.
/// </summary>
public class Result<T> : Result
{
    private readonly T? _value;

    public T Value => IsSuccess
        ? (_value ?? throw new InvalidOperationException("Value cannot be null for a successful result."))
        : throw new InvalidOperationException("Cannot access the value of a failed result.");

    protected internal Result(bool isSuccess, string? error, T? value)
        : base(isSuccess, error)
    {
        _value = value;
    }

    // Allows returning a raw value directly where a Result<T> is expected
    public static implicit operator Result<T>(T value) => Success(value);
}
