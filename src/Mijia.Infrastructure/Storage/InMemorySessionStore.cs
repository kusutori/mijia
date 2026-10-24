using Mijia.Core.Abstractions;
using Mijia.Core.Models;
using Mijia.Core.Results;

namespace Mijia.Infrastructure.Storage;

/// <summary>Process-local session storage. A host can replace it with a credential-vault adapter.</summary>
public sealed class InMemorySessionStore : ISessionStore
{
    private AccountSession? _session;

    public Task<Result<SessionState>> LoadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var session = Volatile.Read(ref _session);
        SessionState state = session is null ? new SignedOut() : new SignedIn(session);
        return Task.FromResult(Result.Ok(state));
    }

    public Task<Result<Unit>> SaveAsync(AccountSession session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        cancellationToken.ThrowIfCancellationRequested();
        Interlocked.Exchange(ref _session, session);
        return Task.FromResult(Result.Ok(Unit.Value));
    }

    public Task<Result<Unit>> ClearAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Interlocked.Exchange(ref _session, null);
        return Task.FromResult(Result.Ok(Unit.Value));
    }
}
