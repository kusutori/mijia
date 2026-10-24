using Mijia.Core.Models;

namespace Mijia.App.Models;

internal enum AppRoute { Home, Settings }
internal enum DeviceKind { Light, AirConditioner, Purifier, Plug, Vacuum, Sensor, Speaker, Camera }
internal enum Appearance { System, Light, Dark }

internal sealed record AppPreferences(Appearance Appearance = Appearance.System, bool CompactCards = false, bool ShowOfflineDevices = true);
internal sealed record DemoAccount(string SessionId, string DisplayName);

internal sealed record DashboardDevice(DeviceInfo Info, DeviceKind Kind, string Room, bool IsOn, int Level, string Reading, string SecondaryReading)
{
    public string Id => Info.Id;
    public bool IsOnline => Info.ConnectionState == DeviceConnectionState.Online;
    public bool HasPower => Kind is DeviceKind.Light or DeviceKind.AirConditioner or DeviceKind.Purifier or DeviceKind.Plug;
    public bool HasLevel => Kind is DeviceKind.Light or DeviceKind.AirConditioner or DeviceKind.Purifier;
    public int Minimum => Kind is DeviceKind.AirConditioner ? 16 : Kind is DeviceKind.Purifier ? 1 : 0;
    public int Maximum => Kind is DeviceKind.AirConditioner ? 30 : Kind is DeviceKind.Purifier ? 3 : 100;
    public string LevelLabel => Kind switch { DeviceKind.AirConditioner => "目标温度", DeviceKind.Purifier => "风速档位", _ => "亮度" };
    public string LevelText => Kind switch { DeviceKind.AirConditioner => $"{Level} °C", DeviceKind.Purifier => $"{Level} 档", _ => $"{Level}%" };
    public string Status => !IsOnline ? "离线" : Kind switch
    {
        DeviceKind.Vacuum => IsOn ? "正在清扫" : "待机中",
        DeviceKind.Sensor => "持续监测",
        DeviceKind.Speaker => "已连接",
        _ => IsOn ? "已开启" : "已关闭"
    };
    public string DisplayReading => Kind switch
    {
        DeviceKind.Light => IsOn ? LevelText : "灯光已关闭",
        DeviceKind.AirConditioner => IsOn ? LevelText : "空调已关闭",
        DeviceKind.Vacuum => IsOn ? "正在清扫" : "电量 86%",
        _ => Reading
    };
}

internal union DeviceInteraction(SetDevicePower, SetDeviceLevel, RunDeviceAction);
internal sealed record SetDevicePower(string DeviceId, bool IsOn);
internal sealed record SetDeviceLevel(string DeviceId, int Level);
internal sealed record RunDeviceAction(string DeviceId);
