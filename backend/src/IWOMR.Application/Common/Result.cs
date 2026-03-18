namespace IWOMR.Application.Common;

/// <summary>
/// Discriminated union result — every use case returns this.
/// Avoids throwing exceptions for expected failures (not found, validation, conflict).
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public ResultError ErrorType { get; }

    private Result(T value)
    {
        IsSuccess = true;
        Value = value;
    }

    private Result(string error, ResultError errorType)
    {
        IsSuccess = false;
        Error = error;
        ErrorType = errorType;
    }

    public static Result<T> Ok(T value) => new(value);
    public static Result<T> NotFound(string error = "Not found") => new(error, ResultError.NotFound);
    public static Result<T> Conflict(string error) => new(error, ResultError.Conflict);
    public static Result<T> Forbidden(string error = "Forbidden") => new(error, ResultError.Forbidden);
    public static Result<T> Fail(string error) => new(error, ResultError.General);
}

public enum ResultError { General, NotFound, Conflict, Forbidden }

/// <summary>Pagination wrapper for list queries.</summary>
public record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize)
{
    public bool HasMore => Page * PageSize < Total;
}

/// <summary>Geo coordinates passed into matching queries.</summary>
public record GeoPoint(double Latitude, double Longitude);
