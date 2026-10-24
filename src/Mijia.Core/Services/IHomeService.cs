using System.Collections.Immutable;
using Mijia.Core.Models;
using Mijia.Core.Results;

namespace Mijia.Core.Services;

public interface IHomeService
{
    Task<Result<ImmutableArray<HomeInfo>>> GetHomesAsync(CancellationToken cancellationToken = default);
}
