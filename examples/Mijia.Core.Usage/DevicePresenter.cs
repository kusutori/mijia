using Mijia.Core.Models;
using Mijia.Core.Results;
using Mijia.Core.Services;

namespace Mijia.Core.Usage;

/// <summary>Compilable UI-consumer example. A ViewModel can use the same constructor contract.</summary>
public sealed class DevicePresenter(IDeviceService devices)
{
    public async Task<string> SetBrightnessAsync(DeviceInfo device, PropertyId brightness,
        int percentage, CancellationToken cancellationToken = default)
    {
        // Resolve brightness's actual PropertyId from the specification; never assume fixed IDs.
        var result = await devices.WritePropertyAsync(device, brightness,
            new IntegerValue(percentage), cancellationToken);

        return result switch
        {
            Success<CommandReceipt> success => success.Value switch
            {
                CommandCompleted => "已执行",
                CommandAccepted => "已接收，等待设备确认",
                null => throw new InvalidOperationException("The adapter returned an empty receipt.")
            },
            Failure failure => DescribeError(failure.Error),
            null => throw new InvalidOperationException("The service returned an empty result.")
        };
    }

    private static string DescribeError(MijiaError error) => error switch
    {
        ValidationError validation => $"参数无效：{validation.Message}",
        AuthenticationError => "请重新登录",
        NetworkError { IsTransient: true } => "连接暂时不可用，请稍后重试",
        NetworkError => "连接失败",
        CloudError cloud => $"云端请求失败：{cloud.Code}",
        DeviceError device => $"设备操作失败：{device.Code}",
        ProtocolError => "服务返回的数据无法识别",
        StorageError => "无法读取或保存登录状态",
        null => throw new InvalidOperationException("The service returned an empty error.")
    };
}
