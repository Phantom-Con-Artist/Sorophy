// Sorophyis Project
// Copyright (C) 2026 Phantom-Con-Artist
//
// This file is part of the Sorophyis Project.
//
// The Sorophyis Project is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// The Sorophyis Project is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU Affero General Public License for more details.
//
// You should have received a copy of the GNU Affero General Public License
// along with the Sorophyis Project. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using Sorophy.Engine.Graph.Evolution.Operations;
using Sorophy.Engine.Graph.History;

namespace Sorophy.Engine.Graph.Evolution;

/// <summary>
/// Executes relationship evolution operations against an <see cref="SorophyGraph"/>.
/// </summary>
/// <remarks>
/// <para>
/// Evolution objects are immutable descriptions of requested transitions.
/// This executor is responsible for applying those transitions to the
/// canonical relationship store.
/// </para>
///
/// <para>
/// For mutations of an existing relationship, the canonical state is
/// captured as an immutable <see cref="SorophyRelationshipFact"/> before the
/// mutation is performed. The historical fact records the evolution's
/// effective time through <see cref="SorophyRelationshipFact.At"/>.
/// </para>
///
/// <para>
/// Relationship creation is different: because no prior relationship state
/// exists, there is no historical fact to capture before creation.
/// </para>
///
/// <para>
/// Deserialization must not invoke this executor implicitly. Evolution
/// execution is an explicit graph operation.
/// </para>
/// </remarks>
public sealed class SorophyRelationshipEvolutionExecutor
{
    /// <summary>
    /// Executes a relationship evolution against the supplied graph.
    /// </summary>
    /// <param name="graph">
    /// The graph whose relationship state will be evolved.
    /// </param>
    /// <param name="evolution">
    /// The evolution operation to execute.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="graph"/> or
    /// <paramref name="evolution"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the evolution type is unsupported.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when an operation targets a relationship that does not exist
    /// or when a relationship creation conflicts with an existing identity.
    /// </exception>
    public void Execute(
        SorophyGraph graph,
        SorophyRelationshipEvolution evolution)
    {
        ArgumentNullException.ThrowIfNull(
            graph);

        ArgumentNullException.ThrowIfNull(
            evolution);

        switch (evolution)
        {
            case SorophyRelationshipCreation creation:
                ExecuteCreation(
                    graph,
                    creation);
                break;

            case SorophyRelationshipTermination termination:
                ExecuteTermination(
                    graph,
                    termination);
                break;

            case SorophyRelationshipTypeChange typeChange:
                ExecuteTypeChange(
                    graph,
                    typeChange);
                break;

            case SorophyRelationshipPropertyModification propertyModification:
                ExecutePropertyModification(
                    graph,
                    propertyModification);
                break;

            case SorophyRelationshipValidityChange validityChange:
                ExecuteValidityChange(
                    graph,
                    validityChange);
                break;

            default:
                throw new ArgumentException(
                    $"Unsupported relationship evolution type '{evolution.GetType().FullName}'.",
                    nameof(evolution));
        }
    }

    /// <summary>
    /// Executes a relationship creation operation.
    /// </summary>
    private static void ExecuteCreation(
        SorophyGraph graph,
        SorophyRelationshipCreation operation)
    {
        if (graph.ContainsRelationship(
                operation.RelationshipId))
        {
            throw new InvalidOperationException(
                $"A relationship with ID '{operation.RelationshipId}' already exists.");
        }

        var relationship =
            new SorophyRelationship
            {
                Id = operation.RelationshipId,
                SourceId = operation.SourceId,
                TargetId = operation.TargetId,
                Type = operation.Type,
                ValidFrom = operation.ValidFrom,
                ValidTill = operation.ValidTill
            };

        foreach (var pair in
                 operation.Properties)
        {
            relationship.Properties.Add(
                pair.Key,
                pair.Value);
        }

        graph.AddRelationship(
            relationship);
    }

    /// <summary>
    /// Executes a relationship termination operation.
    /// </summary>
    private static void ExecuteTermination(
        SorophyGraph graph,
        SorophyRelationshipTermination operation)
    {
        var relationship =
            GetRequiredRelationship(
                graph,
                operation.RelationshipId);

        var fact =
            CreateHistoricalFact(
                relationship,
                operation.EffectiveTime,
                operation.EffectiveTime);

        graph.RecordRelationshipFact(
            fact);

        graph.RemoveRelationship(
            operation.RelationshipId);
    }

    /// <summary>
    /// Executes a relationship type-change operation.
    /// </summary>
    private static void ExecuteTypeChange(
        SorophyGraph graph,
        SorophyRelationshipTypeChange operation)
    {
        var relationship =
            GetRequiredRelationship(
                graph,
                operation.RelationshipId);

        var fact =
            CreateHistoricalFact(
                relationship,
                operation.EffectiveTime,
                operation.EffectiveTime);

        graph.RecordRelationshipFact(
            fact);

        relationship.Type =
            operation.NewType;
    }

    /// <summary>
    /// Executes a relationship property-modification operation.
    /// </summary>
    private static void ExecutePropertyModification(
        SorophyGraph graph,
        SorophyRelationshipPropertyModification operation)
    {
        var relationship =
            GetRequiredRelationship(
                graph,
                operation.RelationshipId);

        var fact =
            CreateHistoricalFact(
                relationship,
                operation.EffectiveTime,
                operation.EffectiveTime);

        graph.RecordRelationshipFact(
            fact);

        foreach (var propertyName in
                 operation.PropertiesToRemove)
        {
            relationship.Properties.Remove(
                propertyName);
        }

        foreach (var pair in
                 operation.PropertiesToSet)
        {
            relationship.Properties[pair.Key] =
                pair.Value;
        }
    }

    /// <summary>
    /// Executes a relationship validity-change operation.
    /// </summary>
    private static void ExecuteValidityChange(
        SorophyGraph graph,
        SorophyRelationshipValidityChange operation)
    {
        var relationship =
            GetRequiredRelationship(
                graph,
                operation.RelationshipId);

        ValidateEffectiveTimeSchema(
            relationship,
            operation.EffectiveTime,
            operation.NewValidFrom,
            operation.NewValidTill);

        var fact =
            CreateHistoricalFact(
                relationship,
                operation.EffectiveTime,
                operation.EffectiveTime);

        graph.RecordRelationshipFact(
            fact);

        relationship.ValidFrom =
            operation.NewValidFrom;

        relationship.ValidTill =
            operation.NewValidTill;
    }

    /// <summary>
    /// Retrieves an active relationship or throws a descriptive exception.
    /// </summary>
    private static SorophyRelationship GetRequiredRelationship(
        SorophyGraph graph,
        Guid relationshipId)
    {
        if (!graph.TryGetRelationship(
                relationshipId,
                out var relationship) ||
            relationship is null)
        {
            throw new InvalidOperationException(
                $"Relationship '{relationshipId}' does not exist in the active graph.");
        }

        return relationship;
    }

    /// <summary>
    /// Creates an immutable historical representation of the current
    /// relationship state immediately before an evolution takes effect.
    /// </summary>
    private static SorophyRelationshipFact CreateHistoricalFact(
        SorophyRelationship relationship,
        Sorophy.Engine.Time.SorophyTime effectiveTime,
        Sorophy.Engine.Time.SorophyTime? historicalValidTill)
    {
        var properties =
            new Dictionary<string, SorophyProperty>(
                relationship.Properties,
                StringComparer.Ordinal);

        return new SorophyRelationshipFact(
            effectiveTime,
            relationship.Id,
            relationship.SourceId,
            relationship.TargetId,
            relationship.Type,
            properties,
            relationship.ValidFrom,
            historicalValidTill);
    }

    /// <summary>
    /// Ensures temporal values supplied by a validity change use the same
    /// schema as the evolution's effective time and the relationship's
    /// existing temporal state.
    /// </summary>
    private static void ValidateEffectiveTimeSchema(
        SorophyRelationship relationship,
        Sorophy.Engine.Time.SorophyTime effectiveTime,
        Sorophy.Engine.Time.SorophyTime? newValidFrom,
        Sorophy.Engine.Time.SorophyTime? newValidTill)
    {
        if (relationship.ValidFrom is not null &&
            !Equals(
                relationship.ValidFrom.Schema,
                effectiveTime.Schema))
        {
            throw new ArgumentException(
                "Evolution effective time and the relationship's ValidFrom must belong to the same temporal schema.",
                nameof(effectiveTime));
        }

        if (relationship.ValidTill is not null &&
            !Equals(
                relationship.ValidTill.Schema,
                effectiveTime.Schema))
        {
            throw new ArgumentException(
                "Evolution effective time and the relationship's ValidTill must belong to the same temporal schema.",
                nameof(effectiveTime));
        }

        if (newValidFrom is not null &&
            !Equals(
                newValidFrom.Schema,
                effectiveTime.Schema))
        {
            throw new ArgumentException(
                "EffectiveTime and NewValidFrom must belong to the same temporal schema.",
                nameof(newValidFrom));
        }

        if (newValidTill is not null &&
            !Equals(
                newValidTill.Schema,
                effectiveTime.Schema))
        {
            throw new ArgumentException(
                "EffectiveTime and NewValidTill must belong to the same temporal schema.",
                nameof(newValidTill));
        }
    }
}