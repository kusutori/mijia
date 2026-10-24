# Reactor UI 原型

## 启动

项目：`src/Mijia.App/Mijia.App.csproj`。

- .NET 11 / C# 15，目标 `net11.0-windows10.0.26100.0`。
- NuGet.org 的 `Microsoft.UI.Reactor` 与 Debug Devtools 固定为 `0.1.0-preview.16`。
- 由 `dotnet new reactor-navview` 初始化，保留模板的 MSIX 模式、启动清单和发布配置。
- 完全使用 C# 声明式组件，没有 XAML 页面或 UI ViewModel。

```powershell
mur check src/Mijia.App/Mijia.App.csproj -- -p:Platform=x64
dotnet run --project src/Mijia.App/Mijia.App.csproj --no-build -p:Platform=x64
```

使用模板的打包启动，需要 Windows 开发人员模式。Visual Studio 中也可以选择 Package 启动配置。

开发工具：

```powershell
dotnet run --project src/Mijia.App/Mijia.App.csproj --no-build -p:Platform=x64 -- --devtools run
mur devtools session list
mur devtools screenshot --out artifacts/ui/home.png
mur devtools shutdown
```

Devtools 仅在 Debug 中引用和启用。关闭正在运行的应用后再重建，以释放构建输出文件。

## 页面和交互

- 登录：欢迎页、二维码登录占位、明确的“体验演示家庭”入口。真实小米登录尚未实现，入口禁用。
- 应用外壳：原生标题栏、Mica、左侧主页/设置导航、演示用户信息；较窄窗口收起导航。
- 主页：8 台演示设备，包含在线与离线状态；房间筛选、设备/房间搜索、刷新、无结果状态和加载状态。
- 设备：台灯/空调/净化器/插座开关，设备详情，亮度/目标温度/风速档位设置，扫拖机器人开始/暂停，音箱提示音模拟反馈。
- 设置：跟随系统/浅色/深色主题、紧凑卡片、显示离线设备、退出确认并回到登录页。

演示音箱不会实际播放声音；所有设备操作都只更新进程内的演示状态。偏好设置在本次进程运行期间保留，退出账号后设备状态重新初始化。

## 数据与状态

```text
AppRoot（账号、偏好）
  ├─ LoginPage
  └─ AppShell（类型安全导航）
       ├─ HomePage（UseResource / UseMutation / 不可变状态覆盖）
       │    ├─ DeviceCard
       │    └─ DeviceDetails
       └─ SettingsPage

PrototypeRuntime → Core.DeviceService → DemoMijiaCloud
                                        ├─ IMijiaCloud
                                        └─ ISpecificationProvider
```

读取使用 `UseResource`，写入使用 `UseMutation`，卸载时取消未完成任务。展示模型是不可变 record；设备控制输入使用 C# 15 联合类型。动态设备、房间控件带稳定键。

窗口标题栏由 `AppRoot` 持有，登录页和主界面共用同一原生 `TitleBar`；导航面板状态也由根组件传入。不要在登录/退出时替换标题栏：当前 Windows App SDK 的旧标题栏可能在卸载后继续收到窗口区域变化回调，触发 `TitleBar.UpdateIconRegion` 中的原生访问冲突。

`DemoMijiaCloud` 放在 App 的 Services 中，未混入生产 Infrastructure。`PrototypeRuntime` 是演示组合入口；设备写入仍经过 Core 的权限、类型、范围和动作参数校验。后续可在组合入口注入真实 `IMijiaCloud` 和规格服务，并把账号登录接到 Core 认证服务。

## 验证

本地使用 `mur check` 验证编译及 Reactor 分析器，并通过运行中的 Debug 应用检查：登录入口、设备电源、搜索、房间筛选、设备详情/滑杆、主题切换、显示离线设备和卡片密度。

标题栏修复通过 WinApp UI 自动化验证：全部 8 张卡片各连续打开/关闭两次，每次关闭后实际拖动窗口，16 轮均正常；用户另行实测确认。

截图和运行日志放在被 Git 忽略的 `artifacts/ui/`。Core 的 54 项行为测试保持独立于 Windows UI 运行。
