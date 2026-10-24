using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Mijia.App;
using Mijia.App.Models;
using Mijia.App.Pages;
using Mijia.App.Services;
using Mijia.App.Styling;
using static Microsoft.UI.Reactor.Factories;

ReactorApp.Run<AppRoot>("米家 · Mijia", width: 1280, height: 900,
    configure: _ => Application.Current.HighContrastAdjustment = ApplicationHighContrastAdjustment.None);

namespace Mijia.App
{
    internal sealed class AppRoot : Component
    {
        public override Element Render()
        {
            var (account, setAccount) = UseState<DemoAccount?>(null);
            var (preferences, updatePreferences) = UseReducer(new AppPreferences());
            var runtime = UseMemo(() => new PrototypeRuntime(), account?.SessionId);
            var expanded = UseBreakpoint(1100);
            var (paneOpen, setPaneOpen) = UseState(true);
            UseEffect(() => setPaneOpen(expanded), expanded);
            var window = UseWindow();
            UseEffect(() =>
            {
                if (window is not null)
                    window.AppWindow.TitleBar.PreferredTheme = preferences.Appearance switch
                    {
                        Appearance.Light => TitleBarTheme.Light,
                        Appearance.Dark => TitleBarTheme.Dark,
                        _ => TitleBarTheme.UseDefaultAppMode
                    };
            }, preferences.Appearance);
            Element content = account is null
                ? Component<LoginPage, LoginPageProps>(new(() => setAccount(new DemoAccount(Guid.NewGuid().ToString("N"), "演示用户"))))
                : Component<AppShell, AppShellProps>(new(account, preferences, updatePreferences, runtime,
                    () => setAccount(null), paneOpen, setPaneOpen));
            // Keep one native TitleBar for the window's lifetime, including across sign-in/sign-out.
            var titleBar = TitleBar("米家 · Mijia").Icon(FontIcon("\uE80F")).Tall()
                .PaneToggleButtonVisible(account is not null).PaneToggleRequested(() => setPaneOpen(!paneOpen));
            return Border(Grid([GridSize.Star()], [GridSize.Auto, GridSize.Star()],
                    titleBar.Grid(0, 0), content.Grid(1, 0)))
                .RequestedTheme(Ui.ThemeFor(preferences.Appearance)).Backdrop(BackdropKind.Mica);
        }
    }
}
