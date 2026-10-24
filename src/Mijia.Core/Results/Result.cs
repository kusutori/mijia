namespace Mijia.Core.Results;

/// <summary>An expected outcome. Cancellation and programming errors remain exceptions.</summary>
public union Result<T>(Success<T>, Failure) where T : notnull
{
    public TResult Match<TResult>(Func<T, TResult> success, Func<MijiaError, TResult> failure)
    {
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);
        return this switch
        {
            Success<T> item => success(item.Value),
            Failure item => failure(item.Error),
            null => throw new InvalidOperationException("An uninitialized result has no outcome.")
        };
    }
}

public sealed record Success<T>(T Value) where T : notnull;

public sealed record Failure(MijiaError Error);

public readonly record struct Unit
{
    public static Unit Value => default;
}

public static class Result
{
    public static Result<T> Ok<T>(T value) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(value);
        return new Success<T>(value);
    }

    public static Result<T> Fail<T>(MijiaError error) where T : notnull
    {
        if (error is null)
            throw new ArgumentException("An error case is required.", nameof(error));

        return new Failure(error);
    }
}
