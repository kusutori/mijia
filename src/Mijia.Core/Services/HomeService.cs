using System.Collections.Immutable;
using Mijia.Core.Abstractions;
using Mijia.Core.Models;
using Mijia.Core.Results;

namespace Mijia.Core.Services;

public sealed class HomeService(IMijiaCloud cloud) : IHomeService
{
    private readonly IMijiaCloud _cloud = cloud ?? throw new ArgumentNullException(nameof(cloud));

    public Task<Result<ImmutableArray<HomeInfo>>> GetHomesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _cloud.GetHomesAsync(cancellationToken);
    }
}
