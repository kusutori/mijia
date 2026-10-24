using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Mijia.App.Models;
using static Microsoft.UI.Reactor.Factories;

namespace Mijia.App.Styling;

internal static class Ui
{
    public static TextBlockElement Icon(string glyph, double size = 24) => TextBlock(glyph)
        .FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets").FontSize(size).AccessibilityHidden();

    public static BorderElement Surface(Element content, bool highContrast) => Border(content)
        .Padding(24).CornerRadius(8).Background(Theme.CardBackground)
        .WithBorder(highContrast ? Theme.Ref("SystemColorWindowTextColorBrush") : Theme.CardStroke, highContrast ? 2 : 1);

    public static BorderElement Badge(string text, bool highContrast) => Border(Caption(text))
        .Padding(8, 4).CornerRadius(4)
        .Background(highContrast ? Theme.Ref("SystemColorWindowColorBrush") : Theme.SubtleFill)
        .WithBorder(highContrast ? Theme.Ref("SystemColorWindowTextColorBrush") : Theme.CardStroke, 1);

    public static TextBlockElement PageTitle(string text) => Title(text).HeadingLevel(AutomationHeadingLevel.Level1);
    public static TextBlockElement SectionTitle(string text) => Subtitle(text).HeadingLevel(AutomationHeadingLevel.Level2);
    public static ScrollViewElement Scroller(Element content) => ScrollView(content)
        .HorizontalContentAlignment(HorizontalAlignment.Stretch).HorizontalScrollMode(ScrollingScrollMode.Disabled);

    // ContentDialog is hosted in a popup; theme its native control rather than its placeholder.
    public static ContentDialogElement DialogTheme(this ContentDialogElement dialog, ElementTheme theme) =>
        dialog.Set(control => control.RequestedTheme = theme);

    public static string Glyph(DeviceKind kind) => kind switch
    {
        DeviceKind.Light => "\uEA80",
        DeviceKind.AirConditioner => "\uE9CA",
        DeviceKind.Purifier => "\uE8BE",
        DeviceKind.Plug => "\uE945",
        DeviceKind.Vacuum => "\uE99A",
        DeviceKind.Sensor => "\uE9CA",
        DeviceKind.Speaker => "\uE767",
        DeviceKind.Camera => "\uE714",
        _ => "\uE80F"
    };

    public static ElementTheme ThemeFor(Appearance appearance) => appearance switch
    {
        Appearance.Light => ElementTheme.Light,
        Appearance.Dark => ElementTheme.Dark,
        _ => ElementTheme.Default
    };
}
