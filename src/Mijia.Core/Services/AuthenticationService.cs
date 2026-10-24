using Mijia.Core.Abstractions;
using Mijia.Core.Models;
using Mijia.Core.Results;

namespace Mijia.Core.Services;

/// <summary>One instance per account. Serializes refresh, sign-in completion and local sign-out.</summary>
public sealed class AuthenticationService(
    IAuthenticationProvider provider,
    ISessionStore store,
    TimeProvider? timeProvider = null) : IAuthenticationService, IDisposable
{
    private readonly IAuthenticationProvider _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    private readonly ISessionStore _store = store ?? throw new ArgumentNullException(nameof(store));
    private readonly TimeProvider _time = timeProvider ?? TimeProvider.System;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public Task<Result<SignInChallenge>> BeginSignInAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _provider.BeginSignInAsync(cancellationToken);
    }

    public async Task<Result<AccountSession>> CompleteSignInAsync(
        SignInChallenge challenge, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(challenge);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (challenge.ExpiresAt <= _time.GetUtcNow())
                return Result.Fail<AccountSession>(new AuthenticationError(AuthenticationFailure.ChallengeExpired, "The sign-in challenge has expired."));

            var result = await _provider.CompleteSignInAsync(challenge, cancellationToken).ConfigureAwait(false);
            return await PersistAsync(result, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<Result<AccountSession>> GetSessionAsync(
        bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var loaded = await _store.LoadAsync(cancellationToken).ConfigureAwait(false);
            if (loaded is Failure failure)
                return failure;

            var state = loaded.Match(static value => value, static _ => throw new InvalidOperationException());
            if (state is SignedOut)
                return Result.Fail<AccountSession>(new AuthenticationError(AuthenticationFailure.SignInRequired, "Sign in to access this account."));

            var session = state switch
            {
                SignedIn signedIn => signedIn.Session,
                SignedOut => throw new InvalidOperationException("Signed-out state was already handled."),
                null => throw new InvalidOperationException("The session store returned an uninitialized state.")
            };

            if (!forceRefresh && (session.ExpiresAt is null || session.ExpiresAt > _time.GetUtcNow()))
                return Result.Ok(session);

            if (string.IsNullOrWhiteSpace(session.RefreshToken))
                return Result.Fail<AccountSession>(new AuthenticationError(AuthenticationFailure.SessionExpired, "The session cannot be refreshed. Sign in again."));

            var refreshed = await _provider.RefreshAsync(session, cancellationToken).ConfigureAwait(false);
            return await PersistAsync(refreshed, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<Result<Unit>> SignOutAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await _store.ClearAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<Result<AccountSession>> PersistAsync(Result<AccountSession> result, CancellationToken cancellationToken)
    {
        if (result is Failure failure)
            return failure;

        var session = result.Match(static value => value, static _ => throw new InvalidOperationException());
        var saved = await _store.SaveAsync(session, cancellationToken).ConfigureAwait(false);
        return saved switch
        {
            Success<Unit> => Result.Ok(session),
            Failure error => error,
            null => throw new InvalidOperationException("The session store returned an uninitialized result.")
        };
    }

    public void Dispose() => _gate.Dispose();
}
