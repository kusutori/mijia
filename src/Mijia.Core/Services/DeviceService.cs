using System.Collections.Immutable;
using Mijia.Core.Abstractions;
using Mijia.Core.Models;
using Mijia.Core.Results;

namespace Mijia.Core.Services;

/// <summary>Uses device snapshots from discovery; property names never act as identifiers.</summary>
public sealed class DeviceService(
    IMijiaCloud cloud,
    ISpecificationProvider specifications,
    IPropertyValueValidator validator) : IDeviceService
{
    private readonly IMijiaCloud _cloud = cloud ?? throw new ArgumentNullException(nameof(cloud));
    private readonly ISpecificationProvider _specifications = specifications ??
        throw new ArgumentNullException(nameof(specifications));
    private readonly IPropertyValueValidator _validator = validator ??
        throw new ArgumentNullException(nameof(validator));

    public Task<Result<ImmutableArray<DeviceInfo>>> GetDevicesAsync(
        string? homeId = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _cloud.GetDevicesAsync(homeId, cancellationToken);
    }

    public Task<Result<DeviceSpecification>> GetSpecificationAsync(
        DeviceInfo device, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(device);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(device.Id))
            return Task.FromResult(Result.Fail<DeviceSpecification>(new ValidationError("device.Id", "A device ID is required.")));

        if (string.IsNullOrWhiteSpace(device.SpecificationType))
            return Task.FromResult(Result.Fail<DeviceSpecification>(new ValidationError("device.SpecificationType", "This device has no MIoT specification type.")));

        return _specifications.GetAsync(device.SpecificationType, cancellationToken);
    }

    public async Task<Result<DeviceValue>> ReadPropertyAsync(
        DeviceInfo device, PropertyId property, CancellationToken cancellationToken = default)
    {
        var specification = await GetSpecificationAsync(device, cancellationToken).ConfigureAwait(false);
        if (specification is Failure failure)
            return failure;

        var target = GetSpecification(specification).FindProperty(property);
        if (target is null || !target.Access.HasFlag(PropertyAccess.Read))
            return Result.Fail<DeviceValue>(new ValidationError("property", "The property does not exist or is not readable."));

        return await _cloud.ReadPropertyAsync(device.Id, property, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result<CommandReceipt>> WritePropertyAsync(
        DeviceInfo device, PropertyId property, DeviceValue value,
        CancellationToken cancellationToken = default)
    {
        var specification = await GetSpecificationAsync(device, cancellationToken).ConfigureAwait(false);
        if (specification is Failure failure)
            return failure;

        var target = GetSpecification(specification).FindProperty(property);
        if (target is null || !target.Access.HasFlag(PropertyAccess.Write))
            return Result.Fail<CommandReceipt>(new ValidationError("property", "The property does not exist or is not writable."));

        if (_validator.Validate(target, value) is Failure invalid)
            return invalid;

        return await _cloud.WritePropertyAsync(device.Id, property, value, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result<ActionExecution>> InvokeActionAsync(
        DeviceInfo device, ActionId action, ImmutableArray<DeviceValue> arguments,
        CancellationToken cancellationToken = default)
    {
        var specification = await GetSpecificationAsync(device, cancellationToken).ConfigureAwait(false);
        if (specification is Failure failure)
            return failure;

        var spec = GetSpecification(specification);
        var target = spec.FindAction(action);
        if (target is null)
            return Result.Fail<ActionExecution>(new ValidationError("action", "The action does not exist."));

        if (arguments.IsDefault)
            arguments = [];

        if (arguments.Length != target.InputPropertyIds.Length)
            return Result.Fail<ActionExecution>(new ValidationError("arguments", "The action argument count does not match its specification."));

        for (var i = 0; i < arguments.Length; i++)
        {
            var property = spec.FindProperty(new PropertyId(action.ServiceId, target.InputPropertyIds[i]));
            if (property is null)
                return Result.Fail<ActionExecution>(new ProtocolError("The action refers to a missing input property."));

            if (_validator.Validate(property, arguments[i]) is Failure invalid)
                return invalid;
        }

        return await _cloud.InvokeActionAsync(device.Id, action, arguments, cancellationToken).ConfigureAwait(false);
    }

    private static DeviceSpecification GetSpecification(Result<DeviceSpecification> result) => result switch
    {
        Success<DeviceSpecification> success => success.Value,
        Failure => throw new InvalidOperationException("The failure must be handled before accessing the specification."),
        null => throw new InvalidOperationException("The specification provider returned an uninitialized result.")
    };
}
