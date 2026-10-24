using Mijia.Core.Models;
using Mijia.Core.Results;

namespace Mijia.Core.Services;

public interface IAuthenticationService
{
    Task<Result<SignInChallenge>> BeginSignInAsync(CancellationToken cancellationToken = default);
    Task<Result<AccountSession>> CompleteSignInAsync(SignInChallenge challenge, CancellationToken cancellationToken = default);
    Task<Result<AccountSession>> GetSessionAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);
    Task<Result<Unit>> SignOutAsync(CancellationToken cancellationToken = default);
}
