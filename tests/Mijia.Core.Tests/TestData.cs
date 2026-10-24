using System.Collections.Immutable;
using Mijia.Core.Abstractions;
using Mijia.Core.Models;
using Mijia.Core.Results;

namespace Mijia.Core.Tests;

internal static class TestData
{
    public const string Type = "urn:miot-spec-v2:device:light:0000A001:example-light:1";
    public static readonly DeviceInfo Device = new("device-1", "Desk light", "example.light", Type);

    public static PropertySpecification Property(string format = "uint8", PropertyAccess access = PropertyAccess.Read | PropertyAccess.Write,
        NumericRange? range = null, ImmutableArray<EnumerationValue> allowed = default) =>
        new(new PropertyId(2, 1), "urn:miot-spec-v2:property:brightness:0000000D:example:1",
            "Brightness", format, access, "percentage", range, allowed.IsDefault ? [] : allowed);

    public static DeviceSpecification Specification(PropertySpecification? property = null) =>
        new(Type, "Example light", [new ServiceSpecification(2, "urn:miot-spec-v2:service:light:00007802:example:1",
            "Light", [property ?? Property()],
            [new ActionSpecification(new ActionId(2, 1), "urn:miot-spec-v2:action:apply:00000001:example:1", "Apply", [1], [1])], [])]);
}

internal sealed class StubSpecifications : ISpecificationProvider
{
    public int Calls;
    public Func<string, CancellationToken, Task<Result<DeviceSpecification>>> Handler { get; set; } =
        (_, _) => Task.FromResult(Result.Ok(TestData.Specification()));

    public Task<Result<DeviceSpecification>> GetAsync(string specificationType, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref Calls);
        return Handler(specificationType, cancellationToken);
    }
}

internal sealed class StubCloud : IMijiaCloud
{
    public int Writes { get; private set; }
    public int Reads { get; private set; }
    public int Actions { get; private set; }
    public string? LastDeviceId { get; private set; }
    public PropertyId LastProperty { get; private set; }
    public DeviceValue LastValue { get; private set; }
    public Result<CommandReceipt> WriteResult { get; set; } = Result.Ok<CommandReceipt>(new CommandAccepted());

    public Task<Result<ImmutableArray<HomeInfo>>> GetHomesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Ok<ImmutableArray<HomeInfo>>([]));

    public Task<Result<ImmutableArray<DeviceInfo>>> GetDevicesAsync(string? homeId = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Ok<ImmutableArray<DeviceInfo>>([TestData.Device]));

    public Task<Result<DeviceValue>> ReadPropertyAsync(string deviceId, PropertyId property, CancellationToken cancellationToken = default)
    {
        Reads++;
        return Task.FromResult(Result.Ok<DeviceValue>(new IntegerValue(42)));
    }

    public Task<Result<CommandReceipt>> WritePropertyAsync(string deviceId, PropertyId property, DeviceValue value,
        CancellationToken cancellationToken = default)
    {
        Writes++;
        LastDeviceId = deviceId;
        LastProperty = property;
        LastValue = value;
        return Task.FromResult(WriteResult);
    }

    public Task<Result<ActionExecution>> InvokeActionAsync(string deviceId, ActionId action,
        ImmutableArray<DeviceValue> arguments, CancellationToken cancellationToken = default)
    {
        Actions++;
        return Task.FromResult(Result.Ok(new ActionExecution(new CommandCompleted(), [])));
    }
}

internal sealed class ManualTimeProvider : TimeProvider
{
    public DateTimeOffset Now { get; set; } = new(2026, 10, 24, 0, 0, 0, TimeSpan.Zero);
    public override DateTimeOffset GetUtcNow() => Now;
}
