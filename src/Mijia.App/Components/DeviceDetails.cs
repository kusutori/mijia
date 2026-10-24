using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Mijia.App.Models;
using Mijia.App.Styling;
using static Microsoft.UI.Reactor.Factories;

namespace Mijia.App.Components;

internal sealed record DeviceDetailsProps(DashboardDevice Device, bool Busy, Action<DeviceInteraction> Interact);

internal sealed class DeviceDetails : Component<DeviceDetailsProps>
{
    public override Element Render()
    {
        var device = Props.Device;
        var (draft, setDraft) = UseState(device.Level);
        UseEffect(() => setDraft(device.Level), device.Level);
        return VStack(24,
            HStack(16, Ui.Icon(Ui.Glyph(device.Kind), 36),
                VStack(4, BodyStrong(device.Info.Name), Caption($"{device.Room} · {device.Status}").Foreground(Theme.SecondaryText))),
            !device.IsOnline ? InfoBar("设备离线", "请检查设备电源及网络连接。").Warning().IsClosable(false) : null,
            Subtitle(device.DisplayReading),
            TextBlock(device.SecondaryReading).Foreground(Theme.SecondaryText),
            device.HasPower ? ToggleSwitch(device.IsOn,
                    on => Props.Interact(new SetDevicePower(device.Id, on)), "已开启", "已关闭", "设备电源")
                .AutomationName($"{device.Info.Name}设备电源").IsEnabled(device.IsOnline && !Props.Busy) : null,
            device.HasLevel ? VStack(12,
                BodyStrong(device.LevelLabel),
                Slider(draft, device.Minimum, device.Maximum, value => setDraft((int)Math.Round(value)))
                    .StepFrequency(1).AutomationName(device.LevelLabel).AutomationId("device-level")
                    .IsEnabled(device.IsOnline && !Props.Busy),
                HStack(16, BodyStrong(device.Kind == DeviceKind.AirConditioner ? $"{draft} °C" : device.Kind == DeviceKind.Purifier ? $"{draft} 档" : $"{draft}%"),
                    Button("应用设置", () => Props.Interact(new SetDeviceLevel(device.Id, draft))).AccentButton()
                        .AutomationId("apply-device-level").IsEnabled(device.IsOnline && !Props.Busy && draft != device.Level))) : null,
            device.Kind is DeviceKind.Vacuum or DeviceKind.Speaker
                ? Button(device.Kind == DeviceKind.Vacuum ? device.IsOn ? "暂停清扫" : "开始清扫" : "播放提示音",
                    () => Props.Interact(new RunDeviceAction(device.Id))).AutomationName($"操作{device.Info.Name}").IsEnabled(device.IsOnline && !Props.Busy) : null,
            Caption("演示模式 · 操作仅更新原型中的设备状态").TextWrapping(Microsoft.UI.Xaml.TextWrapping.WrapWholeWords)
                .Foreground(Theme.SecondaryText))
            .MinWidth(320).AutomationId("device-details");
    }
}
