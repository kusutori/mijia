# Mijia

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

面向米家设备的 .NET 11 / C# 15 Core 与 Windows UI 工程。独立设计模型和接口；不依赖、复制或翻译 Python mijia-api 的实现。

现已包含基于 **Microsoft.UI.Reactor 0.1.0-preview.16** 的 Windows UI 原型：登录、主页设备网格、设置及本地演示交互。[启动和架构说明](docs/ui-prototype.md)。

## 当前状态

| 部分 | 状态 |
| --- | --- |
| 家庭、房间、设备、MIoT 服务/属性/动作/事件模型 | 已实现 |
| 原生联合类型：操作结果、错误、设备值、命令回执、会话状态 | 已实现 |
| 设备服务：读写权限、数值类型、范围、步长、枚举和动作入参校验 | 已实现 |
| 认证服务：登录编排、过期检查、并发刷新合并、本地退出 | 已实现，具体认证协议由 provider 提供 |
| MIoT 公共规格 HTTPS 客户端、容量受限的内存缓存 | 已实现 |
| 内存会话存储 | 已实现；重启后不保留凭证 |
| 小米账号真实扫码登录、Cookie、请求签名/加解密、设备控制 HTTP 适配器 | 后续实现 `IAuthenticationProvider` / `IMijiaCloud` |
| 平台凭证保险库 | 后续实现 `ISessionStore` |

现在可以编译、测试核心业务和规格客户端；还不能直接登录米家账号或控制真实设备。测试使用合成数据和可替换适配器，不需要账号。

## 工程结构

```text
Mijia.slnx
src/
  Mijia.App/          Reactor / WinUI 3 原型与演示适配器
  Mijia.Core/
    Models/          不可变设备、规格、凭证和命令模型
    Results/         Result<T> 和 MijiaError 联合类型
    Abstractions/    云端、认证、规格、存储的适配接口
    Services/        面向 UI 的业务服务接口和实现
  Mijia.Infrastructure/
    Specifications/  公共规格 HTTP 客户端、DTO 映射、缓存
    Serialization/   设备标量 JSON 转换和源生成元数据
    Storage/         内存会话存储
tests/
  Mijia.Core.Tests/  Core 和基础设施的行为测试
examples/
  Mijia.Core.Usage/  可编译的 UI 消费示例
docs/
  architecture.md   分层边界、生命周期和扩展约定
```

`Mijia.Core` 只依赖 .NET BCL；`Mijia.Infrastructure` 依赖 Core。两个生产库都没有第三方 NuGet 依赖。UI 的 ViewModel 依赖服务接口，组合入口负责创建基础设施并注入服务。

## 构建

`global.json` 固定到本机已有的 **11.0.100-rc.1.26425.128**，允许同补丁系列 SDK 更新。目标为 `net11.0`，显式设置 `LangVersion=15.0`，不使用 `preview` 语言开关。升级 SDK 时一并更新该文件。

```powershell
dotnet build src/Mijia.Core/Mijia.Core.csproj -c Release
dotnet test tests/Mijia.Core.Tests/Mijia.Core.Tests.csproj -c Release
mur check src/Mijia.App/Mijia.App.csproj -- -p:Platform=x64
```

## 结果和错误处理

```csharp
public union Result<T>(Success<T>, Failure) where T : notnull;
public sealed record Failure(MijiaError Error);

public union MijiaError(
    ValidationError, AuthenticationError, NetworkError,
    CloudError, DeviceError, ProtocolError, StorageError);
```

预期失败通过结果返回。调用方取消保持 `OperationCanceledException`，由 UI 的任务生命周期处理；空参数、非法构造和未初始化的结果等编程错误保持异常。不要把 `default(Result<T>)` 或 `default(DeviceValue)` 当作有效值。

```csharp
Result<CommandReceipt> result = await devices.WritePropertyAsync(
    device,
    brightnessProperty.Id,
    new IntegerValue(60),
    cancellationToken);

string status = result switch
{
    Success<CommandReceipt> success => success.Value switch
    {
        CommandCompleted => "已执行",
        CommandAccepted => "指令已接收，等待设备确认",
        null => throw new InvalidOperationException("Empty receipt")
    },
    Failure failure => failure.Error switch
    {
        AuthenticationError => "请登录",
        ValidationError validation => validation.Message,
        NetworkError => "网络请求失败",
        CloudError cloud => $"云端错误 {cloud.Code}",
        DeviceError deviceError => $"设备错误 {deviceError.Code}",
        ProtocolError => "数据格式错误",
        StorageError => "会话存储失败",
        null => throw new InvalidOperationException("Empty error")
    },
    null => throw new InvalidOperationException("Empty result")
};
```

完整可编译示例见 `examples/Mijia.Core.Usage/DevicePresenter.cs`。错误信息是诊断文本，UI 可按错误类型、原因或服务端错误码自行本地化。

## 接入方式

通过构造函数注入，容器由 UI 宿主选择。下面的 `cloud` 和 `authenticationProvider` 是后续具体适配器实现：

```csharp
using var http = new HttpClient();
using var specifications = new CachedSpecificationProvider(
    new MiotSpecificationClient(http));

IDeviceService devices = new DeviceService(
    cloud, specifications, new PropertyValueValidator());
IHomeService homes = new HomeService(cloud);

ISessionStore sessionStore = new InMemorySessionStore();
using var authentication = new AuthenticationService(
    authenticationProvider, sessionStore, TimeProvider.System);
```

实际 UI 应在应用/账号作用域持有这些实例，避免每次点击创建 `HttpClient`、认证服务或缓存。Core 不引用 UI 框架、Dispatcher、全局服务定位器或静态账号状态。

设计约定和下一步适配方式见 [架构说明](docs/architecture.md)。

## 协议与语言参考

- [小米 MIoT Spec 文档](https://iot.mi.com/new/doc/design/spec/overall)
- [MIoT 公共规格服务](https://miot-spec.org/miot-spec-v2/instance)
- [.NET 11 RC1 C# 15 发布说明](https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/rc1/csharp.md)
- [C# 原生联合类型参考](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/union)

## 许可证

本项目使用 [MIT 许可证](LICENSE)。第三方依赖遵循各自的许可证。
