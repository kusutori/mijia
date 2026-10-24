using System.Collections.Immutable;
using Mijia.Core.Models;
using Mijia.Core.Results;

namespace Mijia.Core.Services;

public interface IDeviceService
{
    Task<Result<ImmutableArray<DeviceInfo>>> GetDevicesAsync(
        string? homeId = null, CancellationToken cancellationToken = default);

    Task<Result<DeviceSpecification>> GetSpecificationAsync(
        DeviceInfo device, CancellationToken cancellationToken = default);

    Task<Result<DeviceValue>> ReadPropertyAsync(
        DeviceInfo device, PropertyId property, CancellationToken cancellationToken = default);

    Task<Result<CommandReceipt>> WritePropertyAsync(
        DeviceInfo device, PropertyId property, DeviceValue value,
        CancellationToken cancellationToken = default);

    Task<Result<ActionExecution>> InvokeActionAsync(
        DeviceInfo device, ActionId action, ImmutableArray<DeviceValue> arguments,
        CancellationToken cancellationToken = default);
}
