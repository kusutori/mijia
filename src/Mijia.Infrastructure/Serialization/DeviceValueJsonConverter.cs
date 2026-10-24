using System.Buffers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mijia.Core.Models;

namespace Mijia.Infrastructure.Serialization;

/// <summary>Wire conversion stays outside the domain model; integers never pass through double.</summary>
public sealed class DeviceValueJsonConverter : JsonConverter<DeviceValue>
{
    public override DeviceValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.True:
                return new BooleanValue(true);
            case JsonTokenType.False:
                return new BooleanValue(false);
            case JsonTokenType.String:
                return new TextValue(reader.GetString()!);
            case JsonTokenType.Number:
                if (reader.TryGetInt64(out var signed))
                    return new IntegerValue(signed);
                if (reader.TryGetUInt64(out var unsigned))
                    return new UnsignedValue(unsigned);
                ReadOnlySpan<byte> raw = reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan;
                if (raw.IndexOfAny((byte)'.', (byte)'e', (byte)'E') < 0)
                    throw new JsonException("An integer is outside the supported 64-bit range.");
                var number = reader.GetDouble();
                if (!double.IsFinite(number))
                    throw new JsonException("A device number must be finite.");
                return new NumberValue(number);
            default:
                throw new JsonException("A MIoT value must be a boolean, integer, number or string.");
        }
    }

    public override void Write(Utf8JsonWriter writer, DeviceValue value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case BooleanValue boolean:
                writer.WriteBooleanValue(boolean.Value);
                break;
            case IntegerValue integer:
                writer.WriteNumberValue(integer.Value);
                break;
            case UnsignedValue unsigned:
                writer.WriteNumberValue(unsigned.Value);
                break;
            case NumberValue number:
                writer.WriteNumberValue(number.Value);
                break;
            case TextValue text:
                writer.WriteStringValue(text.Value);
                break;
            case null:
                throw new JsonException("An uninitialized device value cannot be serialized.");
        }
    }
}
