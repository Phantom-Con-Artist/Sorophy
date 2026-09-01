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
            2400000L);

        Assert.Equal(OrbValueType.Integer, value.Type);
        Assert.Equal(2400000L, value.Value);
        Assert.IsType<long>(value.Value);
    }

    [Fact]
    public void IntegerValue_ShouldAcceptLongMinValue()
    {
        var value = new OrbValue(
            OrbValueType.Integer,
            long.MinValue);

        Assert.Equal(OrbValueType.Integer, value.Type);
        Assert.Equal(long.MinValue, value.Value);
        Assert.IsType<long>(value.Value);
    }

    [Fact]
    public void IntegerValue_ShouldAcceptLongMaxValue()
    {
        var value = new OrbValue(
            OrbValueType.Integer,
            long.MaxValue);

        Assert.Equal(OrbValueType.Integer, value.Type);
        Assert.Equal(long.MaxValue, value.Value);
        Assert.IsType<long>(value.Value);
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
    public void ListValue_ShouldBeAccepted()
    {
        var list = new List<object?>
        {
            "capital",
            2400000L,
            true,
            null
        };

        var value = new OrbValue(
            OrbValueType.List,
            list);

        Assert.Equal(OrbValueType.List, value.Type);
        Assert.Same(list, value.Value);
        Assert.IsType<List<object?>>(value.Value);
    }

    [Fact]
    public void ObjectValue_ShouldBeAccepted()
    {
        var obj = new Dictionary<string, object?>
        {
            ["name"] = "Avaria",
            ["population"] = 2400000L,
            ["active"] = true,
            ["unknown"] = null
        };

        var value = new OrbValue(
            OrbValueType.Object,
            obj);

        Assert.Equal(OrbValueType.Object, value.Type);
        Assert.Same(obj, value.Value);
        Assert.IsType<Dictionary<string, object?>>(value.Value);
    }

    [Fact]
    public void ListValue_ShouldAllowNestedObjectAndList()
    {
        var nestedList = new List<object?>
        {
            "coastal",
            "capital"
        };

        var nestedObject = new Dictionary<string, object?>
        {
            ["population"] = 2400000L,
            ["active"] = true
        };

        var list = new List<object?>
        {
            nestedList,
            nestedObject
        };

        var value = new OrbValue(
            OrbValueType.List,
            list);

        Assert.Equal(OrbValueType.List, value.Type);
        Assert.Same(list, value.Value);
    }

    [Fact]
    public void ObjectValue_ShouldAllowNestedObjectAndList()
    {
        var obj = new Dictionary<string, object?>
        {
            ["tags"] = new List<object?>
            {
                "capital",
                "coastal"
            },
            ["metadata"] = new Dictionary<string, object?>
            {
                ["population"] = 2400000L,
                ["active"] = true
            }
        };

        var value = new OrbValue(
            OrbValueType.Object,
            obj);

        Assert.Equal(OrbValueType.Object, value.Type);
        Assert.Same(obj, value.Value);
    }

    [Fact]
    public void IntegerType_ShouldRejectInt32()
    {
        Assert.Throws<ArgumentException>(() =>
            new OrbValue(
                OrbValueType.Integer,
                123));
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
    public void IntegerType_ShouldRejectDouble()
    {
        Assert.Throws<ArgumentException>(() =>
            new OrbValue(
                OrbValueType.Integer,
                123.45));
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
    public void ListType_ShouldRejectArray()
    {
        Assert.Throws<ArgumentException>(() =>
            new OrbValue(
                OrbValueType.List,
                new object?[]
                {
                    "A",
                    "B"
                }));
    }

    [Fact]
    public void ListType_ShouldRejectDictionary()
    {
        Assert.Throws<ArgumentException>(() =>
            new OrbValue(
                OrbValueType.List,
                new Dictionary<string, object?>
                {
                    ["name"] = "Avaria"
                }));
    }

    [Fact]
    public void ObjectType_ShouldRejectList()
    {
        Assert.Throws<ArgumentException>(() =>
            new OrbValue(
                OrbValueType.Object,
                new List<object?>
                {
                    "Avaria"
                }));
    }

    [Fact]
    public void ObjectType_ShouldRejectString()
    {
        Assert.Throws<ArgumentException>(() =>
            new OrbValue(
                OrbValueType.Object,
                "Not an object"));
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