using System.Collections.Immutable;
using Mijia.App.Models;
using Mijia.Core.Abstractions;
using Mijia.Core.Models;
using Mijia.Core.Results;

namespace Mijia.App.Services;

/// <summary>Local prototype adapter; it never contacts Xiaomi or operates hardware.</summary>
internal sealed class DemoMijiaCloud : IMijiaCloud, ISpecificationProvider
{
    internal static readonly PropertyId PowerProperty = new(2, 1);
    internal static readonly PropertyId LevelProperty = new(2, 2);
    internal static readonly ActionId PrimaryAction = new(2, 1);
    private readonly object _gate = new();
    private ImmutableArray<DashboardDevice> _devices = CreateDevices();

    public ImmutableArray<DashboardDevice> Snapshot { get { lock (_gate) return _devices; } }

    public Task<Result<ImmutableArray<HomeInfo>>> GetHomesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Result.Ok<ImmutableArray<HomeInfo>>(
            [new HomeInfo("demo-home", "我的家", Snapshot.GroupBy(device => device.Room)
                .Select(room => new RoomInfo(room.Key, room.Key, [.. room.Select(device => device.Id)])).ToImmutableArray())]));
    }

    public async Task<Result<ImmutableArray<DeviceInfo>>> GetDevicesAsync(string? homeId = null, CancellationToken cancellationToken = default)
    {
        await Task.Delay(180, cancellationToken).ConfigureAwait(false);
        return Result.Ok(Snapshot.Where(device => homeId is null || device.Info.HomeId == homeId).Select(device => device.Info).ToImmutableArray());
    }

    public Task<Result<DeviceSpecification>> GetAsync(string specificationType, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var device = Snapshot.FirstOrDefault(item => item.Info.SpecificationType == specificationType);
        if (device is null)
            return Task.FromResult(Result.Fail<DeviceSpecification>(new ProtocolError("Unknown demo specification.")));
        var properties = ImmutableArray.CreateBuilder<PropertySpecification>();
        if (device.HasPower)
            properties.Add(new(PowerProperty, "demo:power", "开关", "bool", PropertyAccess.Read | PropertyAccess.Write, null, null, []));
        if (device.HasLevel)
            properties.Add(new(LevelProperty, "demo:level", device.LevelLabel, "int32", PropertyAccess.Read | PropertyAccess.Write, null,
                new NumericRange(device.Minimum, device.Maximum, 1), []));
        ImmutableArray<ActionSpecification> actions = device.Kind is DeviceKind.Vacuum or DeviceKind.Speaker
            ? [new(PrimaryAction, "demo:primary-action", "执行", [], [])] : [];
        return Task.FromResult(Result.Ok(new DeviceSpecification(specificationType, device.Info.Name,
            [new ServiceSpecification(2, "demo:service", "设备控制", properties.ToImmutable(), actions, [])])));
    }

    public Task<Result<DeviceValue>> ReadPropertyAsync(string deviceId, PropertyId property, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var device = Snapshot.FirstOrDefault(item => item.Id == deviceId);
        if (device is null || !device.IsOnline)
            return Task.FromResult(Result.Fail<DeviceValue>(new DeviceError(deviceId, -1, "设备当前离线")));
        return Task.FromResult(property == PowerProperty ? Result.Ok<DeviceValue>(new BooleanValue(device.IsOn))
            : Result.Ok<DeviceValue>(new IntegerValue(device.Level)));
    }

    public async Task<Result<CommandReceipt>> WritePropertyAsync(string deviceId, PropertyId property, DeviceValue value, CancellationToken cancellationToken = default)
    {
        await Task.Delay(180, cancellationToken).ConfigureAwait(false);
        lock (_gate)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var index = FindIndex(deviceId);
            if (index < 0 || !_devices[index].IsOnline)
                return Result.Fail<CommandReceipt>(new DeviceError(deviceId, -1, "设备当前离线"));
            var device = _devices[index];
            if (property == PowerProperty && device.HasPower && value is BooleanValue power)
                _devices = _devices.SetItem(index, device with { IsOn = power.Value });
            else if (property == LevelProperty && device.HasLevel && value is IntegerValue level)
                _devices = _devices.SetItem(index, device with { Level = (int)level.Value });
            else
                return Result.Fail<CommandReceipt>(new ValidationError("property", "该属性不支持此操作"));
            return Result.Ok<CommandReceipt>(new CommandCompleted());
        }
    }

    public async Task<Result<ActionExecution>> InvokeActionAsync(string deviceId, ActionId action, ImmutableArray<DeviceValue> arguments, CancellationToken cancellationToken = default)
    {
        await Task.Delay(220, cancellationToken).ConfigureAwait(false);
        lock (_gate)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var index = FindIndex(deviceId);
            if (index < 0 || !_devices[index].IsOnline)
                return Result.Fail<ActionExecution>(new DeviceError(deviceId, -1, "设备当前离线"));
            var device = _devices[index];
            if (action != PrimaryAction || device.Kind is not (DeviceKind.Vacuum or DeviceKind.Speaker))
                return Result.Fail<ActionExecution>(new ValidationError("action", "不支持的设备操作"));
            if (device.Kind == DeviceKind.Vacuum)
                _devices = _devices.SetItem(index, device with { IsOn = !device.IsOn, SecondaryReading = device.IsOn ? "清扫已暂停" : "标准清扫 · 正在运行" });
            return Result.Ok(new ActionExecution(new CommandCompleted(), []));
        }
    }

    private int FindIndex(string deviceId)
    {
        for (var i = 0; i < _devices.Length; i++)
            if (_devices[i].Id == deviceId) return i;
        return -1;
    }

    private static ImmutableArray<DashboardDevice> CreateDevices() =>
    [
        Create("light", "米家智能台灯", DeviceKind.Light, "书房", true, 65, "65%", "暖白光 · 4000 K"),
        Create("air", "米家空调", DeviceKind.AirConditioner, "客厅", true, 26, "26 °C", "制冷 · 自动风速"),
        Create("purifier", "空气净化器", DeviceKind.Purifier, "客厅", true, 1, "12 μg/m³", "PM2.5 · 空气优"),
        Create("plug", "米家智能插座", DeviceKind.Plug, "厨房", false, 0, "0.00 kWh", "今日用电"),
        Create("vacuum", "扫拖机器人", DeviceKind.Vacuum, "客厅", false, 0, "电量 86%", "已完成上次清扫"),
        Create("sensor", "温湿度传感器", DeviceKind.Sensor, "卧室", true, 0, "24.6 °C", "相对湿度 48%"),
        Create("speaker", "小爱音箱", DeviceKind.Speaker, "卧室", true, 0, "准备就绪", "等待你的下一句问候"),
        Create("camera", "智能摄像机", DeviceKind.Camera, "玄关", false, 0, "设备离线", "请检查电源和网络", online: false)
    ];

    private static DashboardDevice Create(string id, string name, DeviceKind kind, string room, bool isOn, int level, string reading, string secondary, bool online = true) =>
        new(new DeviceInfo($"demo-{id}", name, $"demo.{id}", $"urn:miot-spec-v2:device:{id}:demo:1", "demo-home", room,
            online ? DeviceConnectionState.Online : DeviceConnectionState.Offline), kind, room, isOn, level, reading, secondary);
}
