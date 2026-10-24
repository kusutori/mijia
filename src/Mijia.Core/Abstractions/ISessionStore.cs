using Mijia.Core.Models;
using Mijia.Core.Results;

namespace Mijia.Core.Abstractions;

/// <summary>A UI host may implement this with its platform credential vault.</summary>
public interface ISessionStore
{
    Task<Result<SessionState>> LoadAsync(CancellationToken cancellationToken = default);
    Task<Result<Unit>> SaveAsync(AccountSession session, CancellationToken cancellationToken = default);
    Task<Result<Unit>> ClearAsync(CancellationToken cancellationToken = default);
}
