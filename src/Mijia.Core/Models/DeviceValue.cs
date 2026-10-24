namespace Mijia.Core.Models;

/// <summary>MIoT scalar values, preserving signed/unsigned 64-bit integer precision.</summary>
public union DeviceValue(BooleanValue, IntegerValue, UnsignedValue, NumberValue, TextValue);

public sealed record BooleanValue(bool Value);
public sealed record IntegerValue(long Value);
public sealed record UnsignedValue(ulong Value);

public sealed record NumberValue
{
    public NumberValue(double value)
    {
        if (!double.IsFinite(value))
            throw new ArgumentOutOfRangeException(nameof(value), "A device number must be finite.");

        Value = value;
    }

    public double Value { get; }
}

public sealed record TextValue
{
    public TextValue(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Value = value;
    }

    public string Value { get; }
}
