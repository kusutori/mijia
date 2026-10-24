using Mijia.Core.Models;
using Mijia.Core.Results;

namespace Mijia.Core.Abstractions;

/// <summary>The provider owns protocol-specific challenge state, cookies and refresh behavior.</summary>
public interface IAuthenticationProvider
{
    Task<Result<SignInChallenge>> BeginSignInAsync(CancellationToken cancellationToken = default);

    Task<Result<AccountSession>> CompleteSignInAsync(
        SignInChallenge challenge, CancellationToken cancellationToken = default);

    Task<Result<AccountSession>> RefreshAsync(
        AccountSession session, CancellationToken cancellationToken = default);
}
