namespace Orb.Engine.Types;

public enum OrbValueType
{
    Null,
    String,
    Boolean,
    Integer,
    Decimal,
    DateTime,
    Guid,
    List,
    Object
}

public sealed class OrbValue
{
    public OrbValueType Type { get; }

    public object? Value { get; }

    public OrbValue(OrbValueType type, object? value)
    {
        ValidateValue(type, value);

        Type = type;
        Value = value;
    }

    private static void ValidateValue(
        OrbValueType type,
        object? value)
    {
        if (type == OrbValueType.Null)
        {
            if (value is not null)
            {
                throw new ArgumentException(
                    "A Null OrbValue must have a null value.");
            }

            return;
        }

        if (value is null)
        {
            throw new ArgumentException(
                $"An OrbValue of type '{type}' cannot have a null value.");
        }

        var isValid = type switch
        {
            OrbValueType.String =>
                value is string,

            OrbValueType.Boolean =>
                value is bool,

            OrbValueType.Integer =>
                value is sbyte
                    or byte
                    or short
                    or ushort
                    or int
                    or uint
                    or long
                    or ulong,

            OrbValueType.Decimal =>
                value is decimal
                    or float
                    or double,

            OrbValueType.DateTime =>
                value is DateTime,

            OrbValueType.Guid =>
                value is Guid,

            OrbValueType.List =>
                value is System.Collections.IEnumerable
                && value is not string,

            OrbValueType.Object =>
                value is System.Collections.IDictionary,

            _ => false
        };

        if (!isValid)
        {
            throw new ArgumentException(
                $"Value of type '{value.GetType().Name}' " +
                $"is not valid for OrbValueType '{type}'.");
        }
    }
}