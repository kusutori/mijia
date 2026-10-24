using System.Collections.Immutable;

namespace Mijia.Core.Models;

public union SessionState(SignedIn, SignedOut);
public sealed record SignedIn(AccountSession Session);
public sealed record SignedOut;

/// <summary>Opaque provider-owned challenge. The UI renders the payload or image URI.</summary>
public sealed record SignInChallenge(
    string Id,
    string QrCodePayload,
    Uri? QrCodeImageUri,
    DateTimeOffset ExpiresAt);

/// <summary>Secret-bearing data; intentionally not a positional record with a generated ToString.</summary>
public sealed class AccountSession
{
    public AccountSession(
        string accountId,
        string serviceToken,
        string securityKey,
        string? refreshToken = null,
        DateTimeOffset? expiresAt = null,
        ImmutableDictionary<string, string>? protocolData = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(securityKey);
        AccountId = accountId;
        ServiceToken = serviceToken;
        SecurityKey = securityKey;
        RefreshToken = refreshToken;
        ExpiresAt = expiresAt;
        ProtocolData = protocolData ?? ImmutableDictionary<string, string>.Empty;
    }

    public string AccountId { get; }
    public string ServiceToken { get; }
    public string SecurityKey { get; }
    public string? RefreshToken { get; }
    public DateTimeOffset? ExpiresAt { get; }
    public ImmutableDictionary<string, string> ProtocolData { get; }

    public override string ToString() => nameof(AccountSession);
}
