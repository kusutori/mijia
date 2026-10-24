using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Mijia.App.Models;
using Mijia.App.Styling;
using static Microsoft.UI.Reactor.Factories;

namespace Mijia.App.Pages;

internal sealed record SettingsPageProps(DemoAccount Account, AppPreferences Preferences,
    Action<Func<AppPreferences, AppPreferences>> UpdatePreferences, Action SignOut);

internal sealed class SettingsPage : Component<SettingsPageProps>
{
    public override Element Render()
    {
        var highContrast = UseHighContrast();
        var (confirmSignOut, setConfirmSignOut) = UseState(false);
        var settings = Props.Preferences;

        var account = Ui.Surface(Grid([GridSize.Auto, GridSize.Star(), GridSize.Auto], [GridSize.Auto],
            PersonPicture().Initials("家").DisplayName(Props.Account.DisplayName).Size(48, 48).Grid(0, 0),
            VStack(4, BodyStrong(Props.Account.DisplayName), Caption("演示家庭 · 本地体验").Foreground(Theme.SecondaryText))
                .Margin(16, 0).VAlign(VerticalAlignment.Center).Grid(0, 1),
            Button("退出登录", () => setConfirmSignOut(true)).AutomationId("sign-out")
                .VAlign(VerticalAlignment.Center).Grid(0, 2)), highContrast);

        var appearance = Ui.Surface(VStack(24,
            SettingsRow("\uE790", "应用主题", "选择浅色、深色，或跟随 Windows。",
                ComboBox(new[] { "跟随系统", "浅色", "深色" }, (int)settings.Appearance,
                    index => Props.UpdatePreferences(current => current with { Appearance = (Appearance)index }))
                    .MinWidth(160).AutomationName("应用主题").AutomationId("theme-selector")),
            Border(null).MinHeight(1).Background(Theme.DividerStroke),
            SettingsRow("\uECA5", "紧凑设备卡片", "在同一屏幕中显示更多设备。",
                ToggleSwitch(settings.CompactCards, value => Props.UpdatePreferences(current => current with { CompactCards = value }), "已开启", "已关闭")
                    .AutomationName("紧凑设备卡片").AutomationId("compact-cards"))), highContrast);

        var devices = Ui.Surface(SettingsRow("\uE839", "显示离线设备", "在主页中保留暂时无法连接的设备。",
            ToggleSwitch(settings.ShowOfflineDevices, value => Props.UpdatePreferences(current => current with { ShowOfflineDevices = value }), "已开启", "已关闭")
                .AutomationName("显示离线设备").AutomationId("show-offline")), highContrast);

        var about = Ui.Surface(HStack(16,
            Ui.Icon("\uE80F", 32),
            VStack(8, BodyStrong("米家 · Mijia"), Caption("一个更从容的家").Foreground(Theme.SecondaryText),
                Ui.Badge("0.1 · 交互原型", highContrast))), highContrast);

        return Grid([GridSize.Star()], [GridSize.Auto, GridSize.Star()],
            VStack(8, Ui.PageTitle("设置"), TextBlock("让这个家，更合你的习惯。").Foreground(Theme.SecondaryText))
                .Margin(32, 24, 32, 24).Grid(0, 0),
            Ui.Scroller(VStack(16,
                    Ui.SectionTitle("账号"), account,
                    Ui.SectionTitle("外观").Margin(0, 12, 0, 0), appearance,
                    Ui.SectionTitle("设备").Margin(0, 12, 0, 0), devices,
                    Ui.SectionTitle("关于").Margin(0, 12, 0, 0), about)
                .MaxWidth(960).HAlign(HorizontalAlignment.Stretch).Margin(32, 0, 32, 32)).Grid(1, 0),
            (ContentDialog("退出演示家庭？", TextBlock("退出后将返回登录页。再次进入会重新载入演示设备。")
                .TextWrapping(TextWrapping.WrapWholeWords), primaryButtonText: "退出登录") with
            {
                IsOpen = confirmSignOut,
                CloseButtonText = "取消",
                OnClosed = result =>
                {
                    setConfirmSignOut(false);
                    if (result == ContentDialogResult.Primary) Props.SignOut();
                }
            }).DialogTheme(Ui.ThemeFor(Props.Preferences.Appearance)))
            .Background(Theme.LayerFill).Landmark(AutomationLandmarkType.Main).AutomationId("settings-page");
    }

    private static Element SettingsRow(string glyph, string title, string subtitle, Element control) =>
        Grid([GridSize.Auto, GridSize.Star(), GridSize.Auto], [GridSize.Auto],
            Ui.Icon(glyph).VAlign(VerticalAlignment.Center).Grid(0, 0),
            VStack(4, BodyStrong(title), Caption(subtitle).TextWrapping(TextWrapping.WrapWholeWords).Foreground(Theme.SecondaryText))
                .Margin(16, 0, 24, 0).VAlign(VerticalAlignment.Center).Grid(0, 1),
            control.VAlign(VerticalAlignment.Center).Grid(0, 2));
}
