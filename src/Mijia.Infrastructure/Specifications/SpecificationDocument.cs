using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mijia.Infrastructure.Specifications;

internal sealed class SpecificationDocument
{
    public required string Type { get; init; }
    public required string Description { get; init; }
    public required ServiceDocument[] Services { get; init; }
}

internal sealed class ServiceDocument
{
    [JsonPropertyName("iid")]
    public required int InstanceId { get; init; }
    public required string Type { get; init; }
    public required string Description { get; init; }
    public PropertyDocument[] Properties { get; init; } = [];
    public ActionDocument[] Actions { get; init; } = [];
    public EventDocument[] Events { get; init; } = [];
}

internal sealed class PropertyDocument
{
    [JsonPropertyName("iid")]
    public required int InstanceId { get; init; }
    public required string Type { get; init; }
    public required string Description { get; init; }
    public required string Format { get; init; }
    public string[] Access { get; init; } = [];
    public string? Unit { get; init; }
    [JsonPropertyName("value-range")]
    public decimal[]? Range { get; init; }
    [JsonPropertyName("value-list")]
    public EnumerationDocument[]? AllowedValues { get; init; }
}

internal sealed class EnumerationDocument
{
    public required JsonElement Value { get; init; }
    public required string Description { get; init; }
}

internal sealed class ActionDocument
{
    [JsonPropertyName("iid")]
    public required int InstanceId { get; init; }
    public required string Type { get; init; }
    public required string Description { get; init; }
    [JsonPropertyName("in")]
    public int[] Inputs { get; init; } = [];
    [JsonPropertyName("out")]
    public int[] Outputs { get; init; } = [];
}

internal sealed class EventDocument
{
    [JsonPropertyName("iid")]
    public required int InstanceId { get; init; }
    public required string Type { get; init; }
    public required string Description { get; init; }
    public int[] Arguments { get; init; } = [];
}
