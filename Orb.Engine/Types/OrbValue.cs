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
        switch (type)
        {
            case OrbValueType.Null:
                if (value is not null)
                {
                    throw new ArgumentException(
                        "Null OrbValue must have a null value.",
                        nameof(value));
                }

                break;

            case OrbValueType.String:
                if (value is not string)
                {
                    throw new ArgumentException(
                        "String OrbValue must contain a string value.",
                        nameof(value));
                }

                break;

            case OrbValueType.Boolean:
                if (value is not bool)
                {
                    throw new ArgumentException(
                        "Boolean OrbValue must contain a boolean value.",
                        nameof(value));
                }

                break;

            case OrbValueType.Integer:
                if (value is not long)
                {
                    throw new ArgumentException(
                        "Integer OrbValue must contain an Int64 (long) value.",
                        nameof(value));
                }

                break;

            case OrbValueType.Decimal:
                if (value is not decimal)
                {
                    throw new ArgumentException(
                        "Decimal OrbValue must contain a Decimal value.",
                        nameof(value));
                }

                break;

            case OrbValueType.DateTime:
                if (value is not DateTime)
                {
                    throw new ArgumentException(
                        "DateTime OrbValue must contain a DateTime value.",
                        nameof(value));
                }

                break;

            case OrbValueType.Guid:
                if (value is not Guid)
                {
                    throw new ArgumentException(
                        "Guid OrbValue must contain a Guid value.",
                        nameof(value));
                }

                break;

            case OrbValueType.List:
                if (value is not List<object?>)
                {
                    throw new ArgumentException(
                        "List OrbValue must contain a List<object?> value.",
                        nameof(value));
                }

                break;

            case OrbValueType.Object:
                if (value is not Dictionary<string, object?>)
                {
                    throw new ArgumentException(
                        "Object OrbValue must contain a Dictionary<string, object?> value.",
                        nameof(value));
                }

                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(type),
                    type,
                    "Unsupported OrbValueType.");
        }
    }
}