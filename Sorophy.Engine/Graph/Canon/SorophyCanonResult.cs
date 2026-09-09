/*
 * Sorophy Engine — a structured knowledge and graph engine
 * Copyright (C) 2026  Subhradeep Sarkar
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published
 * by the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with the Sorophyis Project. If not, see <https://www.gnu.org/licenses/>.
 */

using System;

namespace Sorophy.Engine.Graph.Canon;

/// <summary>
/// Represents the outcome of a Canon Check evaluation against the graph's established temporal history.
/// </summary>
public sealed class SorophyCanonResult
{
    /// <summary>
    /// Gets whether the proposed operation is canonically valid without temporal contradictions.
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// Gets the specific violation kind when <see cref="IsValid"/> is <see langword="false"/>;
    /// otherwise, <see cref="SorophyCanonViolationKind.None"/>.
    /// </summary>
    public SorophyCanonViolationKind ViolationKind { get; }

    /// <summary>
    /// Gets a human-readable explanation of the canon conflict when invalid; otherwise, <see langword="null"/>.
    /// </summary>
    public string? ErrorMessage { get; }

    private SorophyCanonResult(
        bool isValid,
        SorophyCanonViolationKind violationKind,
        string? errorMessage)
    {
        IsValid = isValid;
        ViolationKind = violationKind;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Creates a successful Canon Check result indicating no conflicts.
    /// </summary>
    public static SorophyCanonResult Success() =>
        new(true, SorophyCanonViolationKind.None, null);

    /// <summary>
    /// Creates a failed Canon Check result indicating a temporal conflict.
    /// </summary>
    /// <param name="kind">The violation category.</param>
    /// <param name="message">A descriptive error message.</param>
    public static SorophyCanonResult Conflict(
        SorophyCanonViolationKind kind,
        string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException(
                "A conflict error message cannot be null, empty, or whitespace.",
                nameof(message));
        }

        return new SorophyCanonResult(false, kind, message);
    }

    /// <inheritdoc />
    public override string ToString() =>
        IsValid
            ? "Canon Check: Valid"
            : $"Canon Check: Conflict ({ViolationKind}) — {ErrorMessage}";
}

