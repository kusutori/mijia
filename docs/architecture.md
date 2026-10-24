# Core 架构约定

## 依赖方向

```text
UI / ViewModel ----------> Mijia.Core.Services（面向 UI 的接口）
                                  |
                            Services 实现
                                  |
                     Mijia.Core.Abstractions（适配接口）
                                  ^
                                  |
                       Mijia.Infrastructure
```

UI 的组合入口可以引用 Infrastructure 创建适配器；ViewModel 不处理 HTTP、JSON、Cookie 或签名。Core 内保留 Models / Abstractions / Services 分层，目前无需拆成更多程序集。

## 模型

- 使用不可变 record 和 `ImmutableArray<T>` 表达设备快照、家庭和规格。
- `PropertyId(ServiceId, InstanceId)` 和 `ActionId(ServiceId, InstanceId)` 才是操作标识。名称、描述和型号用于展示，不能作为唯一定位依据。
- `DeviceInfo.SpecificationType` 使用完整设备 URN。规格包含服务层级、属性格式、访问能力、单位、范围、枚举、动作输入/输出、事件参数。
- `DeviceValue` 的五种分支为布尔、有符号整数、无符号整数、浮点数、文本。区分 `long` / `ulong`，不将所有数字统一转为 `double`。
- 不自动把字符串 `"true"`、`"60"` 转成布尔或数值。UI 先按规格转换，再调用服务；这样 JSON 布尔和数字不会被混淆。
- 事件模型是规格元数据，不代表已经实现实时订阅。

## 结果约定

`Result<T>` 使用原生 `union`，成功分支包装为 `Success<T>`，避免泛型 T 与错误类型重叠。错误同样使用联合类型，调用端可以穷尽匹配。

预期的参数、认证、网络、云端、设备、协议、存储失败进入 `Failure`。调用方取消抛出 `OperationCanceledException`。编程错误不被吞成通用“未知错误”。适配器必须返回显式分支，不得返回未初始化的联合值。

`CommandAccepted` 仅表示指令已接收。适配器只有在协议能确认完成时才能返回 `CommandCompleted`。超时或网络中断时，写操作可能已被设备执行，不能由通用重试策略自动重放。

服务端错误码和设备错误码保留原始值；消息供诊断。UI 根据类型和原因显示本地化文案，不用匹配错误字符串。

## 设备操作

`DeviceService` 接受设备列表中的 `DeviceInfo` 快照，避免每次属性操作重新扫描全部家庭和设备。调用时获取规格（通常命中缓存），校验属性访问权限、原始数字格式、范围/步长、枚举或动作入参，再交给 `IMijiaCloud`。

动作入参根据同服务的属性定义和 `in` 顺序校验，不要求这些参数属性本身可写；动作输出由适配器映射到 `ActionExecution.Outputs`。

`IPropertyValueValidator` 可替换，用于扩展新格式或厂商规则。未知格式保留在模型中；默认校验器拒绝写入未知格式，避免错误推断。

## 认证与生命周期

- `IAuthenticationProvider` 负责协议、二维码会话、Cookie、实际登录和刷新。
- `AuthenticationService` 负责登录结果保存、过期判断、刷新串行化和本地退出。每账号共享一个实例；刷新后排队调用重新读取存储，避免重复刷新已更新的凭证。
- `AccountSession` 是凭证对象，`ToString()` 不输出密钥；不可作为普通 UI 数据绑定对象或写入日志。`ProtocolData` 保存适配器需要的额外认证字段。
- 不假定凭证统一有效 30 天。`ExpiresAt` 由 provider 提供，未知时可以为空；服务器拒绝尚未本地过期的凭证时，可使用 `GetSessionAsync(forceRefresh: true)` 刷新。
- `ISessionStore` 可以返回无会话或存储错误。保存失败不会伪装成登录成功；暂时性刷新失败不会删除原凭证。
- 当前 `InMemorySessionStore` 只在进程内保留数据。UI 宿主可接入 Windows 凭证保险库或其他平台等效机制。
- `SignOutAsync` 清理本地会话，不代表服务器端撤销凭证。UI 退出账号时应取消该账号尚未完成的任务，再释放账号作用域。
- Core 的 await 不捕获 UI 同步上下文；UI 自己安排绑定状态更新。公共服务支持 `CancellationToken`。

## Infrastructure

`MiotSpecificationClient` 只访问公开规格 HTTPS 服务，不接触账号凭证。HttpClient 由宿主持有。请求包含完整操作超时，区分调用方取消、超时、HTTP 错误和格式错误；失败消息不包含原始响应正文。

HTTP DTO 留在 Infrastructure 内部，通过 System.Text.Json 源生成读取，再映射为不可变 Core 模型。枚举值使用显式标量转换，Core 不需要 JSON 特性或基础设施引用。

`CachedSpecificationProvider` 缓存成功结果，默认 6 小时、最多 128 个类型；同实例的请求通过异步门串行化，避免重复抓取。允许按类型或整体失效。该实现面向桌面应用的少量设备；大规模服务可替换成并行的按键缓存实现。

两个生产项目开启 `IsAotCompatible`，使用源生成/显式 JSON 元数据，避免依赖反射序列化。此设置启用兼容性分析，不等价于已经完成某个 UI 宿主的 Native AOT 发布验证。

## 后续扩展

1. 在 Infrastructure 实现 `IAuthenticationProvider`，明确保存所有刷新所需的协议字段，针对官方服务实测登录和会话续期。
2. 实现 `IMijiaCloud`：请求认证、签名/加密、家庭/设备查询、属性读写、动作调用、原始错误码映射。分页和共享设备归一化放在适配器内部。
3. 新增平台会话存储，再从 UI 的组合入口注册服务。多账号使用独立认证服务、会话存储和云端适配器。
4. 按需求增加独立的场景、统计、订阅接口及服务，不把所有功能堆进 `DeviceService`。

以上两个云端接口当前没有生产实现，也没有用 `NotImplementedException` 或返回虚假成功的方式冒充真实控制能力。测试替身只存在于测试项目。
