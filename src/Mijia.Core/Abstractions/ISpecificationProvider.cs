using Mijia.Core.Models;
using Mijia.Core.Results;

namespace Mijia.Core.Abstractions;

public interface ISpecificationProvider
{
    Task<Result<DeviceSpecification>> GetAsync(
        string specificationType, CancellationToken cancellationToken = default);
}
