using System.Collections.Immutable;
using Mijia.Core.Models;
using Mijia.Core.Results;

namespace Mijia.Core.Abstractions;

/// <summary>Port for an authenticated cloud adapter. No HTTP or wire DTOs cross this boundary.</summary>
public interface IMijiaCloud
{
    Task<Result<ImmutableArray<HomeInfo>>> GetHomesAsync(CancellationToken cancellationToken = default);

    Task<Result<ImmutableArray<DeviceInfo>>> GetDevicesAsync(
        string? homeId = null, CancellationToken cancellationToken = default);

    Task<Result<DeviceValue>> ReadPropertyAsync(
        string deviceId, PropertyId property, CancellationToken cancellationToken = default);

    Task<Result<CommandReceipt>> WritePropertyAsync(
        string deviceId, PropertyId property, DeviceValue value,
        CancellationToken cancellationToken = default);

    Task<Result<ActionExecution>> InvokeActionAsync(
        string deviceId, ActionId action, ImmutableArray<DeviceValue> arguments,
        CancellationToken cancellationToken = default);
}
