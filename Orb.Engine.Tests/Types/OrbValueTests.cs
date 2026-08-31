using Orb.Engine.Types;

namespace Orb.Engine.Tests.Types;

public class OrbValueTests
{
    [Fact]
    public void StringValue_ShouldBeAccepted()
    {
        var value = new OrbValue(
            OrbValueType.String,
            "Avaria");

        Assert.Equal(OrbValueType.String, value.Type);
        Assert.Equal("Avaria", value.Value);
    }

    [Fact]
    public void IntegerValue_ShouldBeAccepted()
    {
        var value = new OrbValue(
            OrbValueType.Integer,
            2400000);

        Assert.Equal(OrbValueType.Integer, value.Type);
        Assert.Equal(2400000, value.Value);
    }

    [Fact]
    public void DecimalValue_ShouldBeAccepted()
    {
        var value = new OrbValue(
            OrbValueType.Decimal,
            42.75m);

        Assert.Equal(OrbValueType.Decimal, value.Type);
        Assert.Equal(42.75m, value.Value);
    }

    [Fact]
    public void BooleanValue_ShouldBeAccepted()
    {
        var value = new OrbValue(
            OrbValueType.Boolean,
            true);

        Assert.Equal(OrbValueType.Boolean, value.Type);
        Assert.Equal(true, value.Value);
    }

    [Fact]
    public void DateTimeValue_ShouldBeAccepted()
    {
        var date = new DateTime(
            2026,
            8,
            31);

        var value = new OrbValue(
            OrbValueType.DateTime,
            date);

        Assert.Equal(OrbValueType.DateTime, value.Type);
        Assert.Equal(date, value.Value);
    }

    [Fact]
    public void GuidValue_ShouldBeAccepted()
    {
        var id = Guid.NewGuid();

        var value = new OrbValue(
            OrbValueType.Guid,
            id);

        Assert.Equal(OrbValueType.Guid, value.Type);
        Assert.Equal(id, value.Value);
    }

    [Fact]
    public void NullValue_ShouldBeAccepted()
    {
        var value = new OrbValue(
            OrbValueType.Null,
            null);

        Assert.Equal(OrbValueType.Null, value.Type);
        Assert.Null(value.Value);
    }

    [Fact]
    public void IntegerType_ShouldRejectString()
    {
        Assert.Throws<ArgumentException>(() =>
            new OrbValue(
                OrbValueType.Integer,
                "Not an integer"));
    }

    [Fact]
    public void BooleanType_ShouldRejectInteger()
    {
        Assert.Throws<ArgumentException>(() =>
            new OrbValue(
                OrbValueType.Boolean,
                123));
    }

    [Fact]
    public void NullType_ShouldRejectNonNullValue()
    {
        Assert.Throws<ArgumentException>(() =>
            new OrbValue(
                OrbValueType.Null,
                "Not null"));
    }

    [Fact]
    public void NonNullType_ShouldRejectNullValue()
    {
        Assert.Throws<ArgumentException>(() =>
            new OrbValue(
                OrbValueType.String,
                null));
    }
}