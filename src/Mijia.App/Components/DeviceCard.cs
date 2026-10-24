using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using Mijia.App.Models;
using Mijia.App.Styling;
using static Microsoft.UI.Reactor.Factories;

namespace Mijia.App.Components;

internal sealed record DeviceCardProps(DashboardDevice Device, bool Compact, bool Busy,
    Action<DeviceInteraction> Interact, Action<DashboardDevice> ShowDetails);

internal sealed class DeviceCard : Component<DeviceCardProps>
{
    public override Element Render()
    {
        var highContrast = UseHighContrast();
        var device = Props.Device;
        var size = Props.Compact ? 240 : 280;
        var control = device.HasPower
            ? ToggleSwitch(device.IsOn, on => Props.Interact(new SetDevicePower(device.Id, on)), "已开启", "已关闭")
                .AutomationName($"{device.Info.Name}电源").AutomationId($"power-{device.Id}")
                .IsEnabled(device.IsOnline && !Props.Busy)
            : device.Kind is DeviceKind.Vacuum or DeviceKind.Speaker
                ? Button(device.Kind == DeviceKind.Vacuum ? device.IsOn ? "暂停清扫" : "开始清扫" : "播放提示音",
                        () => Props.Interact(new RunDeviceAction(device.Id)))
                    .AutomationName($"操作{device.Info.Name}").AutomationId($"action-{device.Id}").IsEnabled(device.IsOnline && !Props.Busy)
                : (Element)Caption(device.IsOnline ? "数据自动更新" : "暂时无法连接").Foreground(Theme.SecondaryText);

        var content = Grid([GridSize.Star(), GridSize.Auto], [GridSize.Auto, GridSize.Auto, GridSize.Star(), GridSize.Auto],
            Border(Ui.Icon(Ui.Glyph(device.Kind), 28).Foreground(device.IsOnline ? Theme.AccentText : Theme.DisabledText))
                .Padding(12).CornerRadius(8).Background(Theme.SubtleFill)
                .HAlign(HorizontalAlignment.Left).Grid(0, 0),
            Button(Ui.Icon("\uE712", 16), () => Props.ShowDetails(device)).SubtleButton()
                .AutomationName($"{device.Info.Name}详情").ToolTip("设备详情")
                .AutomationId($"details-{device.Id}").VAlign(VerticalAlignment.Top).Grid(0, 1),
            VStack(4, BodyStrong(device.Info.Name), Caption(device.Room).Foreground(Theme.SecondaryText))
                .Margin(0, 16, 0, 0).Grid(1, 0, columnSpan: 2),
            VStack(4, Subtitle(device.DisplayReading), Caption(device.SecondaryReading).Foreground(Theme.SecondaryText))
                .Margin(0, 16, 0, 20).Grid(2, 0, columnSpan: 2),
            control.VAlign(VerticalAlignment.Center).Grid(3, 0),
            Caption(device.IsOnline ? "在线" : "离线").Foreground(device.IsOnline ? Theme.SecondaryText : Theme.DisabledText)
                .VAlign(VerticalAlignment.Center).Grid(3, 1));

        return Ui.Surface(content, highContrast).Width(size).MinHeight(Props.Compact ? 248 : 268)
            .Margin(0, 0, 12, 12).AutomationName($"{device.Info.Name}，{device.Room}，{device.Status}");
    }
}
