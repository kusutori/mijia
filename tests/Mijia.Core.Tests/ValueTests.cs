using System.Text.Json;
using Mijia.Core.Models;
using Mijia.Core.Results;
using Mijia.Core.Services;
using Mijia.Infrastructure.Serialization;

namespace Mijia.Core.Tests;

public sealed class ValueTests
{
    private readonly PropertyValueValidator _validator = new();

    [Theory]
    [InlineData("uint8", -1, false)]
    [InlineData("uint8", 255, true)]
    [InlineData("uint8", 256, false)]
    [InlineData("int8", -128, true)]
    [InlineData("int8", 128, false)]
    [InlineData("unknown-format", 1, false)]
    public void EnforcesIntegerFormats(string format, long number, bool valid)
    {
        var result = _validator.Validate(TestData.Property(format), new IntegerValue(number));
        Assert.Equal(valid, result is Success<Unit>);
    }

    [Fact]
    public void PreservesUInt64MaximumThroughValidation()
    {
        Assert.True(_validator.Validate(TestData.Property("uint64"), new UnsignedValue(ulong.MaxValue)) is Success<Unit>);
        Assert.True(_validator.Validate(TestData.Property("int64"), new UnsignedValue(ulong.MaxValue)) is Failure);
    }

    [Theory]
    [InlineData(0.1, true)]
    [InlineData(0.3, true)]
    [InlineData(0.35, false)]
    [InlineData(1.1, false)]
    public void EnforcesFractionalRangeSteps(double number, bool valid)
    {
        var property = TestData.Property("float", range: new NumericRange(0.1m, 1m, 0.1m));
        Assert.Equal(valid, _validator.Validate(property, new NumberValue(number)) is Success<Unit>);
    }

    [Fact]
    public void ComparesEnumerationNumbersAcrossSignedness()
    {
        var property = TestData.Property("uint8", allowed: [new EnumerationValue(new IntegerValue(1), "One")]);
        Assert.True(_validator.Validate(property, new UnsignedValue(1)) is Success<Unit>);
        Assert.True(_validator.Validate(property, new UnsignedValue(2)) is Failure);
    }

    [Fact]
    public void DoesNotConvertBooleanOrTextToNumbers()
    {
        Assert.True(_validator.Validate(TestData.Property(), new BooleanValue(true)) is Failure);
        Assert.True(_validator.Validate(TestData.Property(), new TextValue("42")) is Failure);
        Assert.True(_validator.Validate(TestData.Property(), new NumberValue(42)) is Failure);
        Assert.True(_validator.Validate(TestData.Property(), default) is Failure);
    }

    [Theory]
    [InlineData("-9223372036854775808")]
    [InlineData("9223372036854775807")]
    [InlineData("18446744073709551615")]
    [InlineData("true")]
    [InlineData("1.25")]
    [InlineData("\"light\"")]
    public void JsonRoundTripDoesNotLoseIntegerPrecision(string json)
    {
        var value = JsonSerializer.Deserialize(json, DeviceValueJson.TypeInfo);
        Assert.Equal(json, JsonSerializer.Serialize(value, DeviceValueJson.TypeInfo));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("18446744073709551616")]
    [InlineData("-9223372036854775809")]
    [InlineData("1e999")]
    public void RejectsInvalidWireValues(string json) =>
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize(json, DeviceValueJson.TypeInfo));

    [Fact]
    public void UninitializedUnionsAreNotSuccessfulResults()
    {
        Result<int> result = default;
        Assert.Throws<InvalidOperationException>(() => result.Match(number => number, _ => -1));
        Assert.Throws<JsonException>(() => JsonSerializer.Serialize(default(DeviceValue), DeviceValueJson.TypeInfo));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void RejectsNonFiniteNumbers(double value) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new NumberValue(value));
}
