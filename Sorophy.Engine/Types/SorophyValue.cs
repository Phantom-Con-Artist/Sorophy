/*
Sorophy™ — a structured knowledge and graph engine
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

namespace Sorophy.Engine.Types;

public enum SorophyValueType
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

public sealed class SorophyValue
{
    public SorophyValueType Type { get; }

    public object? Value { get; }

    public SorophyValue(SorophyValueType type, object? value)
    {
        ValidateValue(type, value);

        Type = type;
        Value = value;
    }

    private static void ValidateValue(
        SorophyValueType type,
        object? value)
    {
        switch (type)
        {
            case SorophyValueType.Null:
                if (value is not null)
                {
                    throw new ArgumentException(
                        "Null SorophyValue must have a null value.",
                        nameof(value));
                }

                break;

            case SorophyValueType.String:
                if (value is not string)
                {
                    throw new ArgumentException(
                        "String SorophyValue must contain a string value.",
                        nameof(value));
                }

                break;

            case SorophyValueType.Boolean:
                if (value is not bool)
                {
                    throw new ArgumentException(
                        "Boolean SorophyValue must contain a boolean value.",
                        nameof(value));
                }

                break;

            case SorophyValueType.Integer:
                if (value is not long)
                {
                    throw new ArgumentException(
                        "Integer SorophyValue must contain an Int64 (long) value.",
                        nameof(value));
                }

                break;

            case SorophyValueType.Decimal:
                if (value is not decimal)
                {
                    throw new ArgumentException(
                        "Decimal SorophyValue must contain a Decimal value.",
                        nameof(value));
                }

                break;

            case SorophyValueType.DateTime:
                if (value is not DateTime)
                {
                    throw new ArgumentException(
                        "DateTime SorophyValue must contain a DateTime value.",
                        nameof(value));
                }

                break;

            case SorophyValueType.Guid:
                if (value is not Guid)
                {
                    throw new ArgumentException(
                        "Guid SorophyValue must contain a Guid value.",
                        nameof(value));
                }

                break;

            case SorophyValueType.List:
                if (value is not List<object?>)
                {
                    throw new ArgumentException(
                        "List SorophyValue must contain a List<object?> value.",
                        nameof(value));
                }

                break;

            case SorophyValueType.Object:
                if (value is not Dictionary<string, object?>)
                {
                    throw new ArgumentException(
                        "Object SorophyValue must contain a Dictionary<string, object?> value.",
                        nameof(value));
                }

                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(type),
                    type,
                    "Unsupported SorophyValueType.");
        }
    }
}