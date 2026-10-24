using System.Globalization;
using Mijia.Core.Models;
using Mijia.Core.Results;

namespace Mijia.Core.Services;

public sealed class PropertyValueValidator : IPropertyValueValidator
{
    public Result<Unit> Validate(PropertySpecification property, DeviceValue value)
    {
        ArgumentNullException.ThrowIfNull(property);

        if (!MatchesFormat(property.Format, value))
            return Invalid($"The value does not match MIoT format '{property.Format}'.");

        if (property.Range is { } range)
        {
            if (!TryDecimal(value, out var number))
                return Invalid("The value cannot be represented by the specified numeric range.");

            if (number < range.Minimum || number > range.Maximum)
                return Invalid($"The value must be between {range.Minimum} and {range.Maximum}.");

            if (range.Step > 0 && (number - range.Minimum) % range.Step != 0)
                return Invalid($"The value must follow step {range.Step} from {range.Minimum}.");
        }

        if (!property.AllowedValues.IsDefaultOrEmpty &&
            !property.AllowedValues.Any(item => Equivalent(item.Value, value)))
            return Invalid("The value is not one of the allowed enumeration values.");

        return Result.Ok(Unit.Value);
    }

    private static bool MatchesFormat(string format, DeviceValue value) => format switch
    {
        "bool" => value is BooleanValue,
        "string" => value is TextValue,
        "int8" => IsIntegerInRange(value, sbyte.MinValue, sbyte.MaxValue),
        "int16" => IsIntegerInRange(value, short.MinValue, short.MaxValue),
        "int32" => IsIntegerInRange(value, int.MinValue, int.MaxValue),
        "int64" => IsIntegerInRange(value, long.MinValue, long.MaxValue),
        "uint8" => IsIntegerInRange(value, byte.MinValue, byte.MaxValue),
        "uint16" => IsIntegerInRange(value, ushort.MinValue, ushort.MaxValue),
        "uint32" => IsIntegerInRange(value, uint.MinValue, uint.MaxValue),
        "uint64" => IsIntegerInRange(value, ulong.MinValue, ulong.MaxValue),
        "float" => value switch
        {
            NumberValue number => Math.Abs(number.Value) <= float.MaxValue,
            IntegerValue or UnsignedValue => true,
            _ => false
        },
        "double" => value is NumberValue or IntegerValue or UnsignedValue,
        _ => false
    };

    private static bool IsIntegerInRange(DeviceValue value, decimal min, decimal max) => value switch
    {
        IntegerValue integer => integer.Value >= min && integer.Value <= max,
        UnsignedValue integer => integer.Value >= min && integer.Value <= max,
        _ => false
    };

    private static bool Equivalent(DeviceValue left, DeviceValue right)
    {
        if (TryDecimal(left, out var first) && TryDecimal(right, out var second))
            return first == second;

        return (left.Value, right.Value) switch
        {
            (BooleanValue a, BooleanValue b) => a.Value == b.Value,
            (TextValue a, TextValue b) => string.Equals(a.Value, b.Value, StringComparison.Ordinal),
            (NumberValue a, NumberValue b) => a.Value == b.Value,
            _ => false
        };
    }

    private static bool TryDecimal(DeviceValue value, out decimal number)
    {
        switch (value)
        {
            case IntegerValue signed:
                number = signed.Value;
                return true;
            case UnsignedValue unsigned:
                number = unsigned.Value;
                return true;
            case NumberValue floating:
                return decimal.TryParse(floating.Value.ToString("R", CultureInfo.InvariantCulture),
                    NumberStyles.Float, CultureInfo.InvariantCulture, out number);
            default:
                number = 0;
                return false;
        }
    }

    private static Result<Unit> Invalid(string message) =>
        Result.Fail<Unit>(new ValidationError("value", message));
}
