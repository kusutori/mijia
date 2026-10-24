using System.Collections.Immutable;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Hooks;
using Microsoft.UI.Reactor.Layout;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Mijia.App.Components;
using Mijia.App.Models;
using Mijia.App.Services;
using Mijia.App.Styling;
using Mijia.Core.Results;
using static Microsoft.UI.Reactor.Factories;

namespace Mijia.App.Pages;

internal sealed record HomePageProps(DemoAccount Account, AppPreferences Preferences, PrototypeRuntime Runtime);

internal sealed class HomePage : Component<HomePageProps>
{
    public override Element Render()
    {
        var highContrast = UseHighContrast();
        var wide = UseBreakpoint(1050);
        var (query, setQuery) = UseState("");
        var (room, setRoom) = UseState("全部房间");
        var (revision, refresh) = UseReducer(0);
        var (overlay, updateOverlay) = UseReducer(ImmutableDictionary<string, DashboardDevice>.Empty);
        var (selectedId, setSelectedId) = UseState<string?>(null);
        var (notice, setNotice) = UseState("");
        var resource = UseResource(ct => Props.Runtime.GetDashboardAsync(ct),
            deps: [Props.Account.SessionId, revision],
            options: new ResourceOptions { StaleTime = TimeSpan.FromMinutes(1) });
        var mutation = UseMutation<DeviceInteraction, Result<DashboardDevice>>(
            (interaction, ct) => Props.Runtime.ExecuteAsync(interaction, ct),
            new MutationOptions<DeviceInteraction, Result<DashboardDevice>>(
                OnSuccess: (result, interaction) =>
                {
                    if (result is Success<DashboardDevice> success)
                    {
                        updateOverlay(current => current.SetItem(success.Value.Id, success.Value));
                        setNotice(interaction switch
                        {
                            SetDeviceLevel => $"{success.Value.Info.Name} · {success.Value.LevelLabel}已更新为 {success.Value.LevelText}",
                            RunDeviceAction when success.Value.Kind == DeviceKind.Speaker => "已模拟播放提示音",
                            _ => $"{success.Value.Info.Name} · {success.Value.Status}"
                        });
                    }
                    else if (result is Failure failure)
                        setNotice(DescribeError(failure.Error));
                }));

        var loaded = resource switch
        {
            AsyncValue<ImmutableArray<DashboardDevice>>.Data data => data.Value,
            AsyncValue<ImmutableArray<DashboardDevice>>.Reloading reloading => reloading.Previous,
            _ => ImmutableArray<DashboardDevice>.Empty
        };
        var devices = loaded.Select(device => overlay.GetValueOrDefault(device.Id, device)).ToImmutableArray();
        var visible = devices.Where(device =>
            (Props.Preferences.ShowOfflineDevices || device.IsOnline) &&
            (room == "全部房间" || device.Room == room) &&
            (string.IsNullOrWhiteSpace(query) || device.Info.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
                device.Room.Contains(query, StringComparison.CurrentCultureIgnoreCase))).ToImmutableArray();
        var selected = devices.FirstOrDefault(device => device.Id == selectedId);
        Action<DeviceInteraction> interact = interaction => _ = mutation.RunAsync(interaction);
        Action refreshDevices = () =>
        {
            updateOverlay(_ => ImmutableDictionary<string, DashboardDevice>.Empty);
            refresh(value => value + 1);
            setNotice("");
        };

        var search = HStack(12,
            TextBox(query, setQuery, placeholderText: "搜索设备或房间")
                .SearchInput().Width(wide ? 240 : 200).VAlign(VerticalAlignment.Center).AutomationName("搜索设备或房间").AutomationId("device-search"),
            Button(HStack(8, Ui.Icon("\uE72C", 16), TextBlock("刷新")), refreshDevices)
                .AutomationName("刷新设备").AutomationId("refresh-devices").VAlign(VerticalAlignment.Center).IsEnabled(!mutation.IsPending));
        var header = Grid(wide ? [GridSize.Star(), GridSize.Auto] : [GridSize.Star()],
            wide ? [GridSize.Auto] : [GridSize.Auto, GridSize.Auto],
            VStack(8, Ui.PageTitle("我的家"), TextBlock("全部设备与常用控制").Foreground(Theme.SecondaryText)).Grid(0, 0),
            search.Margin(0, wide ? 4 : 16, 0, 0).Grid(wide ? 0 : 1, wide ? 1 : 0));

        var summary = Ui.Surface(Grid([GridSize.Star(), GridSize.Star(), GridSize.Star()], [GridSize.Auto],
            Metric(devices.Length.ToString(), "全部设备").Grid(0, 0),
            Metric(devices.Count(device => device.IsOnline).ToString(), "在线设备").Grid(0, 1),
            Metric(devices.Count(device => device.IsOnline && device.IsOn && (device.HasPower || device.Kind == DeviceKind.Vacuum)).ToString(), "正在运行").Grid(0, 2)), highContrast);

        var rooms = new[] { "全部房间" }.Concat(devices.Select(device => device.Room).Distinct()).ToArray();
        var filters = FlexRow(rooms.Select(name => ToggleButton(name, room == name, _ => setRoom(name))
            .AutomationName($"筛选{name}").WithKey(name)).ToArray()) with
        {
            Wrap = FlexWrap.Wrap,
            ColumnGap = 8,
            RowGap = 8
        };

        Element grid = resource switch
        {
            AsyncValue<ImmutableArray<DashboardDevice>>.Loading => VStack(16, ProgressRing(), TextBlock("正在准备你的设备…"))
                .HAlign(HorizontalAlignment.Center).VAlign(VerticalAlignment.Center),
            AsyncValue<ImmutableArray<DashboardDevice>>.Error => VStack(16,
                Ui.Icon("\uE783", 36), Ui.SectionTitle("暂时无法加载设备"),
                Button("重试", refreshDevices).AccentButton()).HAlign(HorizontalAlignment.Center).VAlign(VerticalAlignment.Center),
            _ when visible.Length == 0 => VStack(16,
                Ui.Icon("\uE721", 36), Ui.SectionTitle("没有找到设备"),
                TextBlock("试试其他名称，或切换房间。"),
                Button("查看全部设备", () => { setQuery(""); setRoom("全部房间"); }))
                    .HAlign(HorizontalAlignment.Center).VAlign(VerticalAlignment.Center),
            _ => GridView(visible, device => device.Id, (device, _) =>
                    Component<DeviceCard, DeviceCardProps>(new(device, Props.Preferences.CompactCards, mutation.IsPending,
                        interact, item => setSelectedId(item.Id))).WithKey(device.Id))
                with
            { SelectionMode = ListViewSelectionMode.None }
        };

        var noticeBar = !string.IsNullOrWhiteSpace(notice)
            ? InfoBar(message: notice).Informational().IsClosable(false).AutomationId("device-notice")
            : mutation.Error is not null ? InfoBar("操作未完成", "请稍后再试。").Error().IsClosable(false) : null;

        return Grid([GridSize.Star()],
            [GridSize.Auto, GridSize.Auto, GridSize.Auto, GridSize.Auto, GridSize.Star(), GridSize.Auto],
            header.Margin(0, 0, 0, 24).Grid(0, 0),
            summary.Margin(0, 0, 0, 24).Grid(1, 0),
            filters.Margin(0, 0, 0, 16).Grid(2, 0),
            noticeBar?.Margin(0, 0, 0, 12).Grid(3, 0),
            grid.AutomationName("设备网格").AutomationId("device-grid").Grid(4, 0),
            Caption($"显示 {visible.Length} 台设备 · 演示家庭")
                .Foreground(Theme.SecondaryText).Margin(0, 12, 0, 0).Grid(5, 0),
            selected is not null ? (ContentDialog(selected.Info.Name,
                Component<DeviceDetails, DeviceDetailsProps>(new(selected, mutation.IsPending, interact)), primaryButtonText: "完成") with
            {
                IsOpen = true,
                OnClosed = _ => setSelectedId(null)
            }).DialogTheme(Ui.ThemeFor(Props.Preferences.Appearance)) : null)
            .Padding(32, 24, 20, 24).Background(Theme.LayerFill)
            .Landmark(AutomationLandmarkType.Main).AutomationId("home-page");
    }

    private static Element Metric(string value, string label) => VStack(4, Title(value), Caption(label).Foreground(Theme.SecondaryText));

    private static string DescribeError(MijiaError error) => error switch
    {
        ValidationError validation => validation.Message,
        AuthenticationError => "请重新登录",
        NetworkError => "网络连接暂时不可用",
        CloudError cloud => $"服务暂时不可用（{cloud.Code}）",
        DeviceError device => device.Message,
        ProtocolError => "设备数据暂时无法识别",
        StorageError => "无法保存登录状态",
        null => "操作未完成"
    };
}
