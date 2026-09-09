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
using Sorophy.Engine.Graph.History;
using Sorophy.Engine.Time;

namespace Sorophy.Engine.Graph.Canon;

/// <summary>
/// Authoritative validation engine for Canon Check rules governing temporal entity lifecycle transitions.
/// </summary>
internal static class SorophyCanonValidator
{
    /// <summary>
    /// Validates whether an entity can be canonically created at the specified temporal coordinate.
    /// </summary>
    public static SorophyCanonResult ValidateEntityCreation(
        SorophyGraph graph,
        Guid entityId,
        SorophyTime creationTime)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(creationTime);

        if (entityId == Guid.Empty)
        {
            return SorophyCanonResult.Conflict(
                SorophyCanonViolationKind.EntityNotFound,
                "Entity ID cannot be empty.");
        }

        if (graph.ContainsEntity(entityId))
        {
            return SorophyCanonResult.Conflict(
                SorophyCanonViolationKind.EntityAlreadyExists,
                $"An entity with ID '{entityId}' already exists in the canonical graph.");
        }

        return SorophyCanonResult.Success();
    }

    /// <summary>
    /// Validates whether an entity can be canonically retired at the specified temporal coordinate
    /// without violating established history or referential requirements.
    /// </summary>
    public static SorophyCanonResult ValidateEntityRetirement(
        SorophyGraph graph,
        Guid entityId,
        SorophyTime retirementTime)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(retirementTime);

        if (entityId == Guid.Empty)
        {
            return SorophyCanonResult.Conflict(
                SorophyCanonViolationKind.EntityNotFound,
                "Entity ID cannot be empty.");
        }

        if (!graph.ContainsEntity(entityId))
        {
            return SorophyCanonResult.Conflict(
                SorophyCanonViolationKind.EntityNotFound,
                $"Entity '{entityId}' does not exist in the canonical graph.");
        }

        if (graph.TryGetEntityHistory(entityId, out var history) && history is not null)
        {
            if (history.IsRetired)
            {
                return SorophyCanonResult.Conflict(
                    SorophyCanonViolationKind.AlreadyRetired,
                    $"Entity '{entityId}' already has an established retirement at '{history.RetiredAt}'. " +
                    "To change a retirement coordinate, revert the existing retirement first.");
            }

            if (history.CreatedAt is not null)
            {
                if (!SorophyTime.CanCompare(retirementTime, history.CreatedAt))
                {
                    return SorophyCanonResult.Conflict(
                        SorophyCanonViolationKind.IncompatibleTimeline,
                        $"Retirement coordinate '{retirementTime}' is not comparable with creation coordinate '{history.CreatedAt}'.");
                }

                if (SorophyTime.Compare(retirementTime, history.CreatedAt) < 0)
                {
                    return SorophyCanonResult.Conflict(
                        SorophyCanonViolationKind.PrecedesCreation,
                        $"Retirement coordinate '{retirementTime}' cannot precede creation coordinate '{history.CreatedAt}'.");
                }
            }
        }

        /*
         * Relationship Canon Check:
         * 1. Check direct incident relationship facts recorded in history.
         * If an established relationship-existence fact requires this entity to exist
         * at T' > retirementTime, then retiring the entity at retirementTime creates a contradiction.
         */
        foreach (var relHistory in graph.RelationshipHistories.Values)
        {
            foreach (var fact in relHistory.Facts)
            {
                if (fact.SourceId != entityId && fact.TargetId != entityId)
                {
                    continue;
                }

                if (SorophyTime.CanCompare(fact.At, retirementTime) &&
                    SorophyTime.Compare(fact.At, retirementTime) > 0)
                {
                    // Check if this relationship fact established active existence at fact.At.
                    // In Krono, a fact where fact.ValidTill is null or > fact.At establishes active existence.
                    // (A termination fact sets ValidTill == At, ending the relationship at At).
                    bool isExistenceFact = fact.ValidTill is null ||
                                          SorophyTime.Compare(fact.ValidTill, fact.At) >= 0;

                    if (isExistenceFact)
                    {
                        return SorophyCanonResult.Conflict(
                            SorophyCanonViolationKind.ContradictsIncidentRelationships,
                            $"Cannot retire entity '{entityId}' at '{retirementTime}' because incident relationship " +
                            $"'{fact.RelationshipId}' has established existence at '{fact.At}'.");
                    }
                }
            }
        }

        /*
         * 2. Check established world state across T_established.
         * If an incident relationship in the graph exists and is active at any established
         * coordinate T' > retirementTime, retiring the entity contradicts that established state.
         */
        var establishedCoordinates = GetEstablishedCoordinates(graph);
        foreach (var coord in establishedCoordinates)
        {
            if (SorophyTime.CanCompare(coord, retirementTime) &&
                SorophyTime.Compare(coord, retirementTime) > 0)
            {
                foreach (var rel in graph.Relationships.Values)
                {
                    if (rel.SourceId == entityId || rel.TargetId == entityId)
                    {
                        if (graph.RelationshipExistsAt(rel.Id, coord))
                        {
                            return SorophyCanonResult.Conflict(
                                SorophyCanonViolationKind.ContradictsIncidentRelationships,
                                $"Cannot retire entity '{entityId}' at '{retirementTime}' because incident relationship " +
                                $"'{rel.Id}' has established existence at '{coord}'.");
                        }
                    }
                }
            }
        }

        return SorophyCanonResult.Success();
    }

    /// <summary>
    /// Validates whether an established retirement fact can be canonically reverted.
    /// </summary>
    public static SorophyCanonResult ValidateRetirementReversal(
        SorophyGraph graph,
        Guid entityId,
        SorophyTime retirementTime)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(retirementTime);

        if (entityId == Guid.Empty)
        {
            return SorophyCanonResult.Conflict(
                SorophyCanonViolationKind.EntityNotFound,
                "Entity ID cannot be empty.");
        }

        if (!graph.ContainsEntity(entityId))
        {
            return SorophyCanonResult.Conflict(
                SorophyCanonViolationKind.EntityNotFound,
                $"Entity '{entityId}' does not exist in the canonical graph.");
        }

        if (!graph.TryGetEntityHistory(entityId, out var history) ||
            history is null ||
            !history.IsRetired)
        {
            return SorophyCanonResult.Conflict(
                SorophyCanonViolationKind.RetirementNotFound,
                $"Entity '{entityId}' has no established retirement fact to revert.");
        }

        if (history.RetiredAt is null || !history.RetiredAt.Equals(retirementTime))
        {
            return SorophyCanonResult.Conflict(
                SorophyCanonViolationKind.RetirementNotFound,
                $"Entity '{entityId}' does not have a retirement fact at coordinate '{retirementTime}'. " +
                $"Current established retirement is at '{history.RetiredAt}'.");
        }

        return SorophyCanonResult.Success();
    }

    /// <summary>
    /// Validates whether a relationship can be canonically created at the specified temporal coordinate.
    /// </summary>
    public static SorophyCanonResult ValidateRelationshipCreation(
        SorophyGraph graph,
        Guid relationshipId,
        Guid sourceId,
        Guid targetId,
        SorophyTime creationTime)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(creationTime);

        if (relationshipId == Guid.Empty)
        {
            return SorophyCanonResult.Conflict(
                SorophyCanonViolationKind.RelationshipNotFound,
                "Relationship ID cannot be empty.");
        }

        if (graph.ContainsRelationship(relationshipId))
        {
            return SorophyCanonResult.Conflict(
                SorophyCanonViolationKind.RelationshipAlreadyExists,
                $"A relationship with ID '{relationshipId}' already exists in the canonical graph.");
        }

        if (!graph.ContainsEntity(sourceId))
        {
            return SorophyCanonResult.Conflict(
                SorophyCanonViolationKind.EntityNotFound,
                $"Source entity '{sourceId}' does not exist in the canonical graph.");
        }

        if (!graph.ContainsEntity(targetId))
        {
            return SorophyCanonResult.Conflict(
                SorophyCanonViolationKind.EntityNotFound,
                $"Target entity '{targetId}' does not exist in the canonical graph.");
        }

        if (!graph.EntityExistsAt(sourceId, creationTime))
        {
            return SorophyCanonResult.Conflict(
                SorophyCanonViolationKind.EndpointNotActiveAtCoordinate,
                $"Cannot create relationship '{relationshipId}' at '{creationTime}': source entity '{sourceId}' is not active at this coordinate.");
        }

        if (!graph.EntityExistsAt(targetId, creationTime))
        {
            return SorophyCanonResult.Conflict(
                SorophyCanonViolationKind.EndpointNotActiveAtCoordinate,
                $"Cannot create relationship '{relationshipId}' at '{creationTime}': target entity '{targetId}' is not active at this coordinate.");
        }

        return SorophyCanonResult.Success();
    }

    /// <summary>
    /// Validates whether a relationship can be canonically created at the specified temporal coordinate.
    /// </summary>
    public static SorophyCanonResult ValidateRelationshipCreation(
        SorophyGraph graph,
        SorophyRelationship relationship,
        SorophyTime creationTime)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(relationship);
        ArgumentNullException.ThrowIfNull(creationTime);

        return ValidateRelationshipCreation(
            graph,
            relationship.Id,
            relationship.SourceId,
            relationship.TargetId,
            creationTime);
    }

    /// <summary>
    /// Validates whether a relationship can be canonically retired at the specified temporal coordinate
    /// without violating established history or later active world states.
    /// </summary>
    public static SorophyCanonResult ValidateRelationshipRetirement(
        SorophyGraph graph,
        Guid relationshipId,
        SorophyTime retirementTime)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(retirementTime);

        if (relationshipId == Guid.Empty)
        {
            return SorophyCanonResult.Conflict(
                SorophyCanonViolationKind.RelationshipNotFound,
                "Relationship ID cannot be empty.");
        }

        if (!graph.ContainsRelationship(relationshipId))
        {
            return SorophyCanonResult.Conflict(
                SorophyCanonViolationKind.RelationshipNotFound,
                $"Relationship '{relationshipId}' does not exist in the canonical graph.");
        }

        if (graph.TryGetRelationshipHistory(relationshipId, out var history) && history is not null)
        {
            if (history.IsRetired)
            {
                return SorophyCanonResult.Conflict(
                    SorophyCanonViolationKind.AlreadyRetired,
                    $"Relationship '{relationshipId}' already has an established retirement at '{history.RetiredAt}'. " +
                    "To change a retirement coordinate, revert the existing retirement first.");
            }

            if (history.CreatedAt is not null)
            {
                if (!SorophyTime.CanCompare(retirementTime, history.CreatedAt))
                {
                    return SorophyCanonResult.Conflict(
                        SorophyCanonViolationKind.IncompatibleTimeline,
                        $"Retirement coordinate '{retirementTime}' is not comparable with creation coordinate '{history.CreatedAt}'.");
                }

                if (SorophyTime.Compare(retirementTime, history.CreatedAt) < 0)
                {
                    return SorophyCanonResult.Conflict(
                        SorophyCanonViolationKind.PrecedesCreation,
                        $"Retirement coordinate '{retirementTime}' cannot precede creation coordinate '{history.CreatedAt}'.");
                }
            }
        }

        /*
         * Consequence check over established world state (T_established):
         * Retiring a relationship at T_retire must not contradict any established
         * later active world state at T' in T_established where T' > T_retire.
         */
        var establishedCoordinates = GetEstablishedCoordinates(graph);
        foreach (var coord in establishedCoordinates)
        {
            if (SorophyTime.CanCompare(coord, retirementTime) &&
                SorophyTime.Compare(coord, retirementTime) > 0)
            {
                if (graph.RelationshipExistsAt(relationshipId, coord))
                {
                    return SorophyCanonResult.Conflict(
                        SorophyCanonViolationKind.ContradictsEstablishedLaterState,
                        $"Cannot retire relationship '{relationshipId}' at '{retirementTime}' because it is established as active at later coordinate '{coord}'.");
                }
            }
        }

        return SorophyCanonResult.Success();
    }

    /// <summary>
    /// Validates whether an established retirement fact for a relationship can be canonically reverted.
    /// </summary>
    public static SorophyCanonResult ValidateRelationshipRetirementReversal(
        SorophyGraph graph,
        Guid relationshipId,
        SorophyTime retirementTime)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(retirementTime);

        if (relationshipId == Guid.Empty)
        {
            return SorophyCanonResult.Conflict(
                SorophyCanonViolationKind.RelationshipNotFound,
                "Relationship ID cannot be empty.");
        }

        if (!graph.ContainsRelationship(relationshipId))
        {
            return SorophyCanonResult.Conflict(
                SorophyCanonViolationKind.RelationshipNotFound,
                $"Relationship '{relationshipId}' does not exist in the canonical graph.");
        }

        if (!graph.TryGetRelationshipHistory(relationshipId, out var history) ||
            history is null ||
            !history.IsRetired)
        {
            return SorophyCanonResult.Conflict(
                SorophyCanonViolationKind.RetirementNotFound,
                $"Relationship '{relationshipId}' has no established retirement fact to revert.");
        }

        if (history.RetiredAt is null || !history.RetiredAt.Equals(retirementTime))
        {
            return SorophyCanonResult.Conflict(
                SorophyCanonViolationKind.RetirementNotFound,
                $"Relationship '{relationshipId}' does not have a retirement fact at coordinate '{retirementTime}'. " +
                $"Current established retirement is at '{history.RetiredAt}'.");
        }

        return SorophyCanonResult.Success();
    }

    /// <summary>
    /// Derives the set of all established temporal coordinates across entity and relationship histories.
    /// Used solely by Canon Check for consequence and contradiction detection.
    /// </summary>
    private static IEnumerable<SorophyTime> GetEstablishedCoordinates(SorophyGraph graph)
    {
        var coordinates = new HashSet<SorophyTime>();

        foreach (var history in graph.EntityHistories.Values)
        {
            foreach (var fact in history.Facts)
            {
                coordinates.Add(fact.At);
            }
        }

        foreach (var history in graph.RelationshipHistories.Values)
        {
            foreach (var fact in history.Facts)
            {
                coordinates.Add(fact.At);
            }
        }

        return coordinates;
    }
}

