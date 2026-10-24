using System.Text.Json.Serialization;
using Mijia.Infrastructure.Specifications;

namespace Mijia.Infrastructure.Serialization;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(SpecificationDocument))]
internal partial class MijiaJsonContext : JsonSerializerContext;
