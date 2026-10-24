using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Mijia.Core.Models;

namespace Mijia.Infrastructure.Serialization;

/// <summary>Explicit scalar metadata avoids reflection and the default union object representation.</summary>
public static class DeviceValueJson
{
    public static JsonTypeInfo<DeviceValue> TypeInfo { get; } =
        JsonMetadataServices.CreateValueInfo<DeviceValue>(
            new JsonSerializerOptions { TypeInfoResolver = JsonTypeInfoResolver.Combine() },
            new DeviceValueJsonConverter());
}
