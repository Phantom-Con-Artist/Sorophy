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
        Type = type;
        Value = value;
    }
}