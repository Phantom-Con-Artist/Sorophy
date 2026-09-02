/*
Orb Engine — a structured knowledge and graph engine
Copyright (C) 2026  Subhradeep Sarkar

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published
by the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.

*/

using System.Text.Json;
using Orb.Engine.Types;

namespace Orb.Engine.Serialization;

internal static class OrbValueCodec
{

    private static void ValidateNestedValue(object? value)
{
    if (value is null)
    {
        return;
    }

    switch (value)
    {
        case double doubleValue:
            if (double.IsNaN(doubleValue) ||
                double.IsInfinity(doubleValue))
            {
                throw new InvalidOperationException(
                    "Nested double values must be finite.");
            }

            return;

        case float floatValue:
            if (float.IsNaN(floatValue) ||
                float.IsInfinity(floatValue))
            {
                throw new InvalidOperationException(
                    "Nested float values must be finite.");
            }

            return;

        case IDictionary<string, object?> dictionary:
            foreach (var pair in dictionary)
            {
                ValidateNestedValue(pair.Value);
            }

            return;

        case IEnumerable<object?> list:
            foreach (var item in list)
            {
                ValidateNestedValue(item);
            }

            return;

        case OrbValue orbValue:
            ValidateNestedValue(orbValue.Value);
            return;

        default:
            return;
    }
}
public static JsonElement Serialize(OrbValue value)
{
    ArgumentNullException.ThrowIfNull(value);

    try
    {
        ValidateNestedValue(value.Value);

        return JsonSerializer.SerializeToElement(
            SerializeValue(
                value.Type,
                value.Value));
    }
    catch (InvalidOperationException)
    {
        throw;
    }
    catch (Exception ex)
        when (ex is ArgumentException ||
              ex is NotSupportedException ||
              ex is JsonException ||
              ex is FormatException ||
              ex is OverflowException)
    {
        throw new InvalidOperationException(
            "The OrbValue could not be serialized.",
            ex);
    }
}

    public static OrbValue Deserialize(
        OrbValueType type,
        JsonElement element)
    {
        var value = DeserializeValue(
            type,
            element);

        return new OrbValue(
            type,
            value);
    }

    private static object? SerializeValue(
        OrbValueType type,
        object? value)
    {
        if (type == OrbValueType.Null)
        {
            return null;
        }

        if (value is null)
        {
            throw new InvalidOperationException(
                $"OrbValue of type '{type}' cannot contain a null value.");
        }

        return type switch
        {
            OrbValueType.String =>
                value,

            OrbValueType.Boolean =>
                value,

            OrbValueType.Integer =>
                value,

            OrbValueType.Decimal =>
                value,

            OrbValueType.DateTime =>
                value,

            OrbValueType.Guid =>
                value,

            OrbValueType.List =>
                SerializeList(value),

            OrbValueType.Object =>
                SerializeObject(value),

            _ => throw new InvalidOperationException(
                $"Unsupported OrbValueType '{type}'.")
        };
    }

    private static List<JsonElement> SerializeList(
        object value)
    {
        if (value is not IEnumerable<object?> list)
        {
            throw new InvalidOperationException(
                "List OrbValue must contain an enumerable object collection.");
        }

        var result = new List<JsonElement>();

        foreach (var item in list)
        {
            var nested = InferOrbValue(item);

            result.Add(
                SerializeNestedValue(
                    nested.OrbValue,
                    nested.ClrType));
        }

        return result;
    }

    private static Dictionary<string, JsonElement> SerializeObject(
        object value)
    {
        if (value is not IDictionary<string, object?> dictionary)
        {
            throw new InvalidOperationException(
                "Object OrbValue must contain a string-keyed object dictionary.");
        }

        var result =
            new Dictionary<string, JsonElement>();

        foreach (var pair in dictionary)
        {
            var nested = InferOrbValue(pair.Value);

            result[pair.Key] =
                SerializeNestedValue(
                    nested.OrbValue,
                    nested.ClrType);
        }

        return result;
    }

    private static JsonElement SerializeNestedValue(
        OrbValue value,
        string? clrType)
    {
        return JsonSerializer.SerializeToElement(
            new NestedValueDocument
            {
                Type = value.Type.ToString(),

                ClrType = clrType,

                Value = SerializeValue(
                    value.Type,
                    value.Value)
            });
    }

    private static InferredOrbValue InferOrbValue(
        object? value)
    {
        if (value is OrbValue orbValue)
        {
            return new InferredOrbValue(
                orbValue,
                null);
        }

        if (value is null)
        {
            return new InferredOrbValue(
                new OrbValue(
                    OrbValueType.Null,
                    null),
                null);
        }

        return value switch
        {
            string stringValue =>
                new InferredOrbValue(
                    new OrbValue(
                        OrbValueType.String,
                        stringValue),
                    null),

            bool booleanValue =>
                new InferredOrbValue(
                    new OrbValue(
                        OrbValueType.Boolean,
                        booleanValue),
                    null),

            byte byteValue =>
                new InferredOrbValue(
                    new OrbValue(
                        OrbValueType.Integer,
                        (long)byteValue),
                    nameof(Byte)),

            sbyte sbyteValue =>
                new InferredOrbValue(
                    new OrbValue(
                        OrbValueType.Integer,
                        (long)sbyteValue),
                    nameof(SByte)),

            short shortValue =>
                new InferredOrbValue(
                    new OrbValue(
                        OrbValueType.Integer,
                        (long)shortValue),
                    nameof(Int16)),

            ushort ushortValue =>
                new InferredOrbValue(
                    new OrbValue(
                        OrbValueType.Integer,
                        (long)ushortValue),
                    nameof(UInt16)),

            int intValue =>
                new InferredOrbValue(
                    new OrbValue(
                        OrbValueType.Integer,
                        (long)intValue),
                    nameof(Int32)),

            uint uintValue =>
                new InferredOrbValue(
                    new OrbValue(
                        OrbValueType.Integer,
                        (long)uintValue),
                    nameof(UInt32)),

            long longValue =>
                new InferredOrbValue(
                    new OrbValue(
                        OrbValueType.Integer,
                        longValue),
                    nameof(Int64)),

            ulong ulongValue =>
                new InferredOrbValue(
                    new OrbValue(
                        OrbValueType.Integer,
                        checked((long)ulongValue)),
                    nameof(UInt64)),

            decimal decimalValue =>
                new InferredOrbValue(
                    new OrbValue(
                        OrbValueType.Decimal,
                        decimalValue),
                    nameof(Decimal)),

            float floatValue =>
                new InferredOrbValue(
                    new OrbValue(
                        OrbValueType.Decimal,
                        (decimal)floatValue),
                    nameof(Single)),

            double doubleValue =>
                new InferredOrbValue(
                    new OrbValue(
                        OrbValueType.Decimal,
                        (decimal)doubleValue),
                    nameof(Double)),

            DateTime dateTimeValue =>
                new InferredOrbValue(
                    new OrbValue(
                        OrbValueType.DateTime,
                        dateTimeValue),
                    null),

            Guid guidValue =>
                new InferredOrbValue(
                    new OrbValue(
                        OrbValueType.Guid,
                        guidValue),
                    null),

            IDictionary<string, object?> objectValue =>
                new InferredOrbValue(
                    new OrbValue(
                        OrbValueType.Object,
                        objectValue.ToDictionary(
                            pair => pair.Key,
                            pair => pair.Value)),
                    null),

            IEnumerable<object?> listValue =>
                new InferredOrbValue(
                    new OrbValue(
                        OrbValueType.List,
                        listValue.ToList()),
                    null),

            _ => throw new InvalidOperationException(
                $"Unsupported nested CLR type '{value.GetType().FullName}'.")
        };
    }

    private static object? DeserializeValue(
        OrbValueType type,
        JsonElement element)
    {
        try
        {
            return type switch
            {
                OrbValueType.Null =>
                    DeserializeNull(element),

                OrbValueType.String =>
                    DeserializeString(element),

                OrbValueType.Boolean =>
                    DeserializeBoolean(element),

                OrbValueType.Integer =>
                    DeserializeInteger(element),

                OrbValueType.Decimal =>
                    DeserializeDecimal(element),

                OrbValueType.DateTime =>
                    DeserializeDateTime(element),

                OrbValueType.Guid =>
                    DeserializeGuid(element),

                OrbValueType.List =>
                    DeserializeList(element),

                OrbValueType.Object =>
                    DeserializeObject(element),

                _ => throw new InvalidOperationException(
                    $"Unsupported OrbValueType '{type}'.")
            };
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
            when (ex is FormatException ||
                  ex is OverflowException ||
                  ex is ArgumentException)
        {
            throw new InvalidOperationException(
                $"Invalid value for OrbValueType '{type}'.",
                ex);
        }
    }

    private static object? DeserializeNull(
        JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Null)
        {
            throw new InvalidOperationException(
                "Null OrbValue must be represented by JSON null.");
        }

        return null;
    }

    private static string DeserializeString(
        JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException(
                "String OrbValue must be represented by a JSON string.");
        }

        return element.GetString()
            ?? throw new InvalidOperationException(
                "String OrbValue cannot contain a null value.");
    }

    private static bool DeserializeBoolean(
        JsonElement element)
    {
        if (element.ValueKind is not
            (JsonValueKind.True or JsonValueKind.False))
        {
            throw new InvalidOperationException(
                "Boolean OrbValue must be represented by a JSON boolean.");
        }

        return element.GetBoolean();
    }

    private static long DeserializeInteger(
        JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Number ||
            !element.TryGetInt64(out var value))
        {
            throw new InvalidOperationException(
                "Integer OrbValue must be represented by a valid Int64 JSON number.");
        }

        return value;
    }

    private static decimal DeserializeDecimal(
        JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Number ||
            !element.TryGetDecimal(out var value))
        {
            throw new InvalidOperationException(
                "Decimal OrbValue must be represented by a valid Decimal JSON number.");
        }

        return value;
    }

    private static DateTime DeserializeDateTime(
        JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.String ||
            !element.TryGetDateTime(out var value))
        {
            throw new InvalidOperationException(
                "DateTime OrbValue must be represented by a valid DateTime JSON string.");
        }

        return value;
    }

    private static Guid DeserializeGuid(
        JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.String ||
            !element.TryGetGuid(out var value))
        {
            throw new InvalidOperationException(
                "Guid OrbValue must be represented by a valid GUID JSON string.");
        }

        return value;
    }

    private static List<object?> DeserializeList(
        JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException(
                "List OrbValue must be represented by a JSON array.");
        }

        var result = new List<object?>();

        foreach (var item in element.EnumerateArray())
        {
            var nested =
                DeserializeNestedValue(item);

            result.Add(
                nested.Value);
        }

        return result;
    }

    private static Dictionary<string, object?> DeserializeObject(
        JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException(
                "Object OrbValue must be represented by a JSON object.");
        }

        var result =
            new Dictionary<string, object?>();

        foreach (var property in element.EnumerateObject())
        {
            var nested =
                DeserializeNestedValue(
                    property.Value);

            result[property.Name] =
                nested.Value;
        }

        return result;
    }

    private static DecodedNestedValue DeserializeNestedValue(
        JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException(
                "Nested OrbValue must be represented by a typed JSON object.");
        }

        if (!element.TryGetProperty(
                "Type",
                out var typeElement))
        {
            throw new InvalidOperationException(
                "Nested OrbValue is missing its type.");
        }

        if (!element.TryGetProperty(
                "Value",
                out var valueElement))
        {
            throw new InvalidOperationException(
                "Nested OrbValue is missing its value.");
        }

        if (typeElement.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException(
                "Nested OrbValue type must be a JSON string.");
        }

        var typeName =
            typeElement.GetString();

        if (!Enum.TryParse<OrbValueType>(
                typeName,
                ignoreCase: true,
                out var type))
        {
            throw new InvalidOperationException(
                $"Unknown nested OrbValueType '{typeName}'.");
        }

        string? clrType = null;

        if (element.TryGetProperty(
                "ClrType",
                out var clrTypeElement) &&
            clrTypeElement.ValueKind != JsonValueKind.Null)
        {
            if (clrTypeElement.ValueKind !=
                JsonValueKind.String)
            {
                throw new InvalidOperationException(
                    "Nested OrbValue ClrType must be a JSON string or null.");
            }

            clrType =
                clrTypeElement.GetString();
        }

        var canonicalValue =
            DeserializeValue(
                type,
                valueElement);

        var restoredValue =
            RestoreClrPrimitiveType(
                type,
                clrType,
                canonicalValue);

        return new DecodedNestedValue(
            type,
            restoredValue);
    }

    private static object? RestoreClrPrimitiveType(
        OrbValueType type,
        string? clrType,
        object? value)
    {
        if (string.IsNullOrWhiteSpace(clrType))
        {
            return value;
        }

        if (value is null)
        {
            throw new InvalidOperationException(
                $"Nested CLR type '{clrType}' cannot contain null.");
        }

        if (type == OrbValueType.Integer)
        {
            if (value is not long integerValue)
            {
                throw new InvalidOperationException(
                    "Integer value could not be restored from its canonical representation.");
            }

            try
            {
                return clrType switch
                {
                    nameof(Byte) =>
                        checked((byte)integerValue),

                    nameof(SByte) =>
                        checked((sbyte)integerValue),

                    nameof(Int16) =>
                        checked((short)integerValue),

                    nameof(UInt16) =>
                        checked((ushort)integerValue),

                    nameof(Int32) =>
                        checked((int)integerValue),

                    nameof(UInt32) =>
                        checked((uint)integerValue),

                    nameof(Int64) =>
                        integerValue,

                    nameof(UInt64) =>
                        checked((ulong)integerValue),

                    _ => throw new InvalidOperationException(
                        $"Unsupported nested integer CLR type '{clrType}'.")
                };
            }
            catch (OverflowException ex)
            {
                throw new InvalidOperationException(
                    $"Nested integer value '{integerValue}' cannot be represented as '{clrType}'.",
                    ex);
            }
        }

        if (type == OrbValueType.Decimal)
        {
            if (value is not decimal decimalValue)
            {
                throw new InvalidOperationException(
                    "Decimal value could not be restored from its canonical representation.");
            }

            try
            {
                return clrType switch
                {
                    nameof(Single) =>
                        (float)decimalValue,

                    nameof(Double) =>
                        (double)decimalValue,

                    nameof(Decimal) =>
                        decimalValue,

                    _ => throw new InvalidOperationException(
                        $"Unsupported nested decimal CLR type '{clrType}'.")
                };
            }
            catch (OverflowException ex)
            {
                throw new InvalidOperationException(
                    $"Nested decimal value '{decimalValue}' cannot be represented as '{clrType}'.",
                    ex);
            }
        }

        throw new InvalidOperationException(
            $"CLR subtype metadata '{clrType}' is not valid for OrbValueType '{type}'.");
    }

    private sealed class NestedValueDocument
    {
        public string Type { get; set; } = string.Empty;

        public string? ClrType { get; set; }

        public object? Value { get; set; }
    }

    private sealed record InferredOrbValue(
        OrbValue OrbValue,
        string? ClrType);

    private sealed record DecodedNestedValue(
        OrbValueType Type,
        object? Value);
}