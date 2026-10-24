using Mijia.Core.Abstractions;
using Mijia.Core.Models;
using Mijia.Core.Results;
using Mijia.Core.Services;
using Mijia.Infrastructure.Storage;

namespace Mijia.Core.Tests;

public sealed class AuthenticationServiceTests
{
    [Fact]
    public async Task MissingSessionRequiresSignIn()
    {
        using var service = new AuthenticationService(new StubAuthentication(), new InMemorySessionStore());
        Assert.True(await service.GetSessionAsync() is Failure
        {
            Error: AuthenticationError { Reason: AuthenticationFailure.SignInRequired }
        });
    }

    [Fact]
    public async Task ConcurrentExpiredSessionRequestsRefreshOnlyOnce()
    {
        var time = new ManualTimeProvider();
        var store = new InMemorySessionStore();
        await store.SaveAsync(Session(time.Now.AddSeconds(-1)));
        var provider = new StubAuthentication
        {
            RefreshHandler = async (_, token) =>
            {
                await Task.Yield();
                token.ThrowIfCancellationRequested();
                return Result.Ok(Session(time.Now.AddHours(1)));
            }
        };
        using var service = new AuthenticationService(provider, store, time);
        var results = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => service.GetSessionAsync()));
        Assert.All(results, result => Assert.True(result is Success<AccountSession>));
        Assert.Equal(1, provider.RefreshCalls);
    }

    [Fact]
    public async Task TransientRefreshFailurePreservesStoredSession()
    {
        var time = new ManualTimeProvider();
        var store = new InMemorySessionStore();
        var original = Session(time.Now.AddSeconds(-1));
        await store.SaveAsync(original);
        var provider = new StubAuthentication
        {
            RefreshHandler = (_, _) => Task.FromResult(Result.Fail<AccountSession>(new NetworkError("Unavailable", IsTransient: true)))
        };
        using var service = new AuthenticationService(provider, store, time);
        Assert.True(await service.GetSessionAsync() is Failure { Error: NetworkError });
        var stored = await store.LoadAsync();
        Assert.True(stored is Success<SessionState> { Value: SignedIn });
        var state = stored.Match(value => value, _ => throw new InvalidOperationException());
        Assert.Same(original, state is SignedIn signedIn ? signedIn.Session : null);
    }

    [Fact]
    public async Task ExpiredChallengeDoesNotCallProvider()
    {
        var time = new ManualTimeProvider();
        var provider = new StubAuthentication();
        using var service = new AuthenticationService(provider, new InMemorySessionStore(), time);
        var challenge = new SignInChallenge("id", "qr", null, time.Now.AddSeconds(-1));
        Assert.True(await service.CompleteSignInAsync(challenge) is Failure
        {
            Error: AuthenticationError { Reason: AuthenticationFailure.ChallengeExpired }
        });
        Assert.Equal(0, provider.CompleteCalls);
    }

    [Fact]
    public async Task SignInCannotReportSuccessWhenSessionStorageFails()
    {
        var time = new ManualTimeProvider();
        var provider = new StubAuthentication();
        using var service = new AuthenticationService(provider, new FailingSessionStore(), time);
        Assert.True(await service.CompleteSignInAsync(new SignInChallenge("id", "qr", null, time.Now.AddMinutes(1)))
            is Failure { Error: StorageError });
    }

    [Fact]
    public async Task ForceRefreshHandlesServerInvalidationBeforeLocalExpiry()
    {
        var store = new InMemorySessionStore();
        await store.SaveAsync(Session());
        var provider = new StubAuthentication();
        using var service = new AuthenticationService(provider, store);
        await service.GetSessionAsync();
        Assert.Equal(0, provider.RefreshCalls);
        await service.GetSessionAsync(forceRefresh: true);
        Assert.Equal(1, provider.RefreshCalls);
    }

    [Fact]
    public async Task SignOutClearsTheSession()
    {
        var store = new InMemorySessionStore();
        await store.SaveAsync(Session());
        using var service = new AuthenticationService(new StubAuthentication(), store);
        Assert.True(await service.SignOutAsync() is Success<Unit>);
        Assert.True(await service.GetSessionAsync() is Failure { Error: AuthenticationError });
    }

    [Fact]
    public void SessionToStringDoesNotDiscloseCredentials()
    {
        var session = Session();
        Assert.DoesNotContain("secret-service", session.ToString());
        Assert.DoesNotContain("secret-key", new SignedIn(session).ToString());
        Assert.DoesNotContain("secret-refresh", Result.Ok(session).ToString());
    }

    private static AccountSession Session(DateTimeOffset? expires = null) =>
        new("account", "secret-service", "secret-key", "secret-refresh", expires);

    private sealed class StubAuthentication : IAuthenticationProvider
    {
        public int RefreshCalls;
        public int CompleteCalls;
        public Func<AccountSession, CancellationToken, Task<Result<AccountSession>>> RefreshHandler { get; set; } =
            (_, _) => Task.FromResult(Result.Ok(Session()));

        public Task<Result<SignInChallenge>> BeginSignInAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Ok(new SignInChallenge("id", "qr", null, DateTimeOffset.UtcNow.AddMinutes(1))));

        public Task<Result<AccountSession>> CompleteSignInAsync(SignInChallenge challenge, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref CompleteCalls);
            return Task.FromResult(Result.Ok(Session()));
        }

        public Task<Result<AccountSession>> RefreshAsync(AccountSession session, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref RefreshCalls);
            return RefreshHandler(session, cancellationToken);
        }
    }

    private sealed class FailingSessionStore : ISessionStore
    {
        public Task<Result<SessionState>> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Ok<SessionState>(new SignedOut()));
        public Task<Result<Unit>> SaveAsync(AccountSession session, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Fail<Unit>(new StorageError("Unavailable")));
        public Task<Result<Unit>> ClearAsync(CancellationToken cancellationToken = default) => Task.FromResult(Result.Ok(Unit.Value));
    }
}
