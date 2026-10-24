using Mijia.Core.Models;
using Mijia.Core.Results;
using Mijia.Core.Services;

namespace Mijia.Core.Tests;

public sealed class DeviceServiceTests
{
    [Fact]
    public async Task InvalidWriteDoesNotReachCloud()
    {
        var cloud = new StubCloud();
        var service = new DeviceService(cloud, new StubSpecifications(), new PropertyValueValidator());
        var result = await service.WritePropertyAsync(TestData.Device, new PropertyId(2, 1), new IntegerValue(256));
        Assert.True(result is Failure { Error: ValidationError });
        Assert.Equal(0, cloud.Writes);
    }

    [Fact]
    public async Task ReadOnlyPropertyCannotBeWritten()
    {
        var cloud = new StubCloud();
        var specifications = new StubSpecifications
        {
            Handler = (_, _) => Task.FromResult(Result.Ok(TestData.Specification(TestData.Property(access: PropertyAccess.Read))))
        };
        var service = new DeviceService(cloud, specifications, new PropertyValueValidator());
        Assert.True(await service.WritePropertyAsync(TestData.Device, new PropertyId(2, 1), new IntegerValue(42)) is Failure);
        Assert.Equal(0, cloud.Writes);
    }

    [Fact]
    public async Task PreservesAcceptedReceiptAndExactAddress()
    {
        var cloud = new StubCloud();
        var service = new DeviceService(cloud, new StubSpecifications(), new PropertyValueValidator());
        var result = await service.WritePropertyAsync(TestData.Device, new PropertyId(2, 1), new IntegerValue(42));
        Assert.True(result is Success<CommandReceipt> { Value: CommandAccepted });
        Assert.Equal(TestData.Device.Id, cloud.LastDeviceId);
        Assert.Equal(new PropertyId(2, 1), cloud.LastProperty);
        Assert.True(cloud.LastValue is IntegerValue { Value: 42 });
    }

    [Fact]
    public async Task InvalidActionArgumentsDoNotReachCloud()
    {
        var cloud = new StubCloud();
        var service = new DeviceService(cloud, new StubSpecifications(), new PropertyValueValidator());
        Assert.True(await service.InvokeActionAsync(TestData.Device, new ActionId(2, 1), []) is Failure);
        Assert.True(await service.InvokeActionAsync(TestData.Device, new ActionId(2, 1), [new TextValue("42")]) is Failure);
        Assert.Equal(0, cloud.Actions);
        Assert.True(await service.InvokeActionAsync(TestData.Device, new ActionId(2, 1), [new IntegerValue(42)]) is Success<ActionExecution>);
        Assert.Equal(1, cloud.Actions);
    }

    [Fact]
    public async Task PropagatesCloudFailureWithoutConvertingToSuccess()
    {
        var cloud = new StubCloud
        {
            WriteResult = Result.Fail<CommandReceipt>(new DeviceError(TestData.Device.Id, -1, "Offline"))
        };
        var service = new DeviceService(cloud, new StubSpecifications(), new PropertyValueValidator());
        var result = await service.WritePropertyAsync(TestData.Device, new PropertyId(2, 1), new IntegerValue(42));
        Assert.True(result is Failure { Error: DeviceError { Code: -1 } });
    }

    [Fact]
    public async Task MissingSpecificationFailsBeforeNetwork()
    {
        var provider = new StubSpecifications();
        var service = new DeviceService(new StubCloud(), provider, new PropertyValueValidator());
        Assert.True(await service.GetSpecificationAsync(TestData.Device with { SpecificationType = null }) is Failure);
        Assert.Equal(0, provider.Calls);
    }

    [Fact]
    public async Task CancellationIsAnExceptionNotABusinessFailure()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var provider = new StubSpecifications();
        var service = new DeviceService(new StubCloud(), provider, new PropertyValueValidator());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.ReadPropertyAsync(TestData.Device, new PropertyId(2, 1), cancellation.Token));
        Assert.Equal(0, provider.Calls);
    }
}
