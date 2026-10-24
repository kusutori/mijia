using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Navigation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Mijia.App.Models;
using Mijia.App.Pages;
using Mijia.App.Services;
using Mijia.App.Styling;
using static Microsoft.UI.Reactor.Factories;

namespace Mijia.App;

internal sealed record AppShellProps(DemoAccount Account, AppPreferences Preferences,
    Action<Func<AppPreferences, AppPreferences>> UpdatePreferences, PrototypeRuntime Runtime, Action SignOut,
    bool PaneOpen, Action<bool> SetPaneOpen);

internal sealed class AppShell : Component<AppShellProps>
{
    public override Element Render()
    {
        var navigation = UseNavigation(AppRoute.Home);
        var paneOpen = Props.PaneOpen;
        var expanded = UseBreakpoint(1100);
        var highContrast = UseHighContrast();
        var pages = NavigationHost(navigation, route => route switch
        {
            AppRoute.Settings => Component<SettingsPage, SettingsPageProps>(new(Props.Account, Props.Preferences, Props.UpdatePreferences, Props.SignOut)),
            _ => Component<HomePage, HomePageProps>(new(Props.Account, Props.Preferences, Props.Runtime))
        }) with
        { Transition = NavigationTransition.None };
        var footer = VStack(12,
            Ui.Badge("交互原型 · 演示模式", highContrast),
            HStack(12, PersonPicture().Initials("家").DisplayName(Props.Account.DisplayName).Size(36, 36),
                VStack(4, BodyStrong(Props.Account.DisplayName), Caption("我的家 · 8 台设备").Foreground(Theme.SecondaryText))))
            .Margin(12, 16);
        var navigationView = (NavigationView([
                NavItem("主页", icon: "Home", tag: "home"), NavItem("设置", icon: "Setting", tag: "settings")
            ], pages)
            .WithNavigation(navigation, route => route == AppRoute.Settings ? "settings" : "home", tag => tag == "settings" ? AppRoute.Settings : AppRoute.Home)
            .PaneDisplayMode(expanded ? NavigationViewPaneDisplayMode.Left : NavigationViewPaneDisplayMode.LeftCompact)
            .IsPaneOpen(paneOpen, Props.SetPaneOpen).OpenPaneLength(232)
            .PaneHeader(paneOpen ? VStack(4, BodyStrong("我的智能家庭"), Caption("让日常，简单一点").Foreground(Theme.SecondaryText)).Margin(16, 12) : Empty())
            .PaneFooter(paneOpen ? footer : Empty()).PaneToggleButtonVisible(false).BackButtonVisible(false).TitleBarAutoPadding(false)
            .Landmark(AutomationLandmarkType.Navigation).AutomationName("主导航").Flex(grow: 1, basis: 0)) with
        { IsSettingsVisible = false };
        return navigationView;
    }
}
