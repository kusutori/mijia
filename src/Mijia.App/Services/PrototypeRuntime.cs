using System.Collections.Immutable;
using Mijia.App.Models;
using Mijia.Core.Models;
using Mijia.Core.Results;
using Mijia.Core.Services;

namespace Mijia.App.Services;

/// <summary>Demo composition root. Commands use the Core validation/service pipeline.</summary>
internal sealed class PrototypeRuntime
{
    private readonly DemoMijiaCloud _cloud = new();
    private readonly IDeviceService _devices;
    public PrototypeRuntime() => _devices = new DeviceService(_cloud, _cloud, new PropertyValueValidator());

    public async Task<ImmutableArray<DashboardDevice>> GetDashboardAsync(CancellationToken cancellationToken)
    {
        var result = await _devices.GetDevicesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        return result.Match(devices => devices.Select(info => _cloud.Snapshot.First(item => item.Id == info.Id)).ToImmutableArray(),
            _ => throw new InvalidOperationException("The demo device catalog is unavailable."));
    }

    public async Task<Result<DashboardDevice>> ExecuteAsync(DeviceInteraction interaction, CancellationToken cancellationToken)
    {
        var id = interaction switch
        {
            SetDevicePower power => power.DeviceId,
            SetDeviceLevel level => level.DeviceId,
            RunDeviceAction action => action.DeviceId,
            null => throw new ArgumentException("An interaction is required.", nameof(interaction))
        };
        var device = _cloud.Snapshot.FirstOrDefault(item => item.Id == id);
        if (device is null)
            return Result.Fail<DashboardDevice>(new ValidationError("device", "找不到设备"));
        switch (interaction)
        {
            case SetDevicePower power:
                if (await _devices.WritePropertyAsync(device.Info, DemoMijiaCloud.PowerProperty, new BooleanValue(power.IsOn), cancellationToken).ConfigureAwait(false) is Failure powerFailure)
                    return powerFailure;
                break;
            case SetDeviceLevel level:
                if (await _devices.WritePropertyAsync(device.Info, DemoMijiaCloud.LevelProperty, new IntegerValue(level.Level), cancellationToken).ConfigureAwait(false) is Failure levelFailure)
                    return levelFailure;
                break;
            case RunDeviceAction:
                if (await _devices.InvokeActionAsync(device.Info, DemoMijiaCloud.PrimaryAction, [], cancellationToken).ConfigureAwait(false) is Failure actionFailure)
                    return actionFailure;
                break;
        }
        return Result.Ok(_cloud.Snapshot.First(item => item.Id == id));
    }
}
