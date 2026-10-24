namespace Mijia.Core.Results;

/// <summary>Stable categories that UI callers can handle exhaustively.</summary>
public union MijiaError(
    ValidationError,
    AuthenticationError,
    NetworkError,
    CloudError,
    DeviceError,
    ProtocolError,
    StorageError);

public sealed record ValidationError(string Field, string Message);

public sealed record AuthenticationError(AuthenticationFailure Reason, string Message);

public enum AuthenticationFailure
{
    SignInRequired,
    SessionExpired,
    ChallengeExpired,
    Rejected
}

public sealed record NetworkError(string Message, int? HttpStatusCode = null, bool IsTransient = false);

public sealed record CloudError(int Code, string Message);

public sealed record DeviceError(string DeviceId, int Code, string Message);

public sealed record ProtocolError(string Message);

public sealed record StorageError(string Message);
