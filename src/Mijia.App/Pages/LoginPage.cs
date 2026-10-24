using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Mijia.App.Styling;
using static Microsoft.UI.Reactor.Factories;

namespace Mijia.App.Pages;

internal sealed record LoginPageProps(Action EnterDemo);

internal sealed class LoginPage : Component<LoginPageProps>
{
    public override Element Render()
    {
        var wide = UseBreakpoint(1000);
        var highContrast = UseHighContrast();
        var hero = VStack(28,
            Border(Ui.Icon("\uE80F", 40)).Padding(20).CornerRadius(8).Background(Theme.CardBackground).HAlign(HorizontalAlignment.Left),
            VStack(12,
                TitleLarge("欢迎回家").HeadingLevel(AutomationHeadingLevel.Level1),
                BodyLarge("你的设备，你的日常。\n在一个地方，轻松照顾整个家。")
                    .TextWrapping(TextWrapping.WrapWholeWords).Foreground(Theme.SecondaryText)),
            Grid([GridSize.Star(), GridSize.Star()], [GridSize.Auto, GridSize.Auto],
                PreviewTile("\uEA80", "恰好的灯光", "书房 · 亮度 65%", highContrast).Grid(0, 0),
                PreviewTile("\uE9CA", "舒适的温度", "客厅 · 26 °C", highContrast).Grid(0, 1),
                PreviewTile("\uE8BE", "清新的空气", "PM2.5 · 优", highContrast).Grid(1, 0),
                PreviewTile("\uE767", "熟悉的陪伴", "小爱音箱 · 已连接", highContrast).Grid(1, 1)))
            .MaxWidth(480).VAlign(VerticalAlignment.Center).Margin(0, 0, 48, 0);

        var signIn = Ui.Surface(VStack(24,
            VStack(8, Ui.SectionTitle("连接你的智能家庭"),
                TextBlock("登录后，在这里查看和管理米家设备。")
                    .TextWrapping(TextWrapping.WrapWholeWords).Foreground(Theme.SecondaryText)),
            Border(VStack(16,
                    Ui.Icon("\uED14", 64).HAlign(HorizontalAlignment.Center),
                    BodyStrong("二维码登录"),
                    Caption("账号服务接入后启用").Foreground(Theme.SecondaryText))
                .HAlign(HorizontalAlignment.Center).VAlign(VerticalAlignment.Center))
                .Size(224, 224).HAlign(HorizontalAlignment.Center)
                .Background(Theme.SubtleFill).CornerRadius(8).WithBorder(Theme.CardStroke, highContrast ? 2 : 1),
            VStack(12,
                Button("小米账号登录").HorizontalContentAlignment(HorizontalAlignment.Center)
                    .HAlign(HorizontalAlignment.Stretch).MinHeight(44).IsEnabled(false)
                    .ToolTip("真实账号登录将在下一步接入"),
                Button("体验演示家庭", Props.EnterDemo).AccentButton()
                    .HAlign(HorizontalAlignment.Stretch).MinHeight(44).AutomationId("enter-demo")),
            Caption("当前为交互原型，演示操作不会控制真实设备。")
                .TextWrapping(TextWrapping.WrapWholeWords).Foreground(Theme.SecondaryText)), highContrast)
            .MaxWidth(400).HAlign(HorizontalAlignment.Center).VAlign(VerticalAlignment.Center);

        var content = Grid(wide ? [GridSize.Star(), GridSize.Star()] : [GridSize.Star()], [GridSize.Star()],
            wide ? hero.Grid(0, 0) : null,
            signIn.Grid(0, wide ? 1 : 0)).MaxWidth(1100).Margin(36).HAlign(HorizontalAlignment.Stretch);

        return Grid([GridSize.Star()], [GridSize.Star(), GridSize.Auto],
            Ui.Scroller(content).Grid(0, 0),
            Caption("MIJIA  /  一个更从容的家")
                .Foreground(Theme.SecondaryText).HAlign(HorizontalAlignment.Center).Margin(24).Grid(1, 0))
            .AutomationId("login-page");
    }

    private static Element PreviewTile(string glyph, string title, string detail, bool highContrast) =>
        Ui.Surface(VStack(12, Ui.Icon(glyph).Foreground(Theme.AccentText), BodyStrong(title),
            Caption(detail).Foreground(Theme.SecondaryText)), highContrast).Margin(0, 0, 12, 12);
}
