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
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using Sorophy.Engine.Graph;
using Sorophy.Engine.Time;

namespace Sorophy.Engine.Tests.TemporalQuery;

/// <summary>
/// Shared test fixtures and builder utilities for the TQD Crucible adversarial test suite.
/// </summary>
internal static class TqdCrucibleTestHelper
{
    public static SorophyTimeSchema CreateNumericSchema(
        string timeline = "Crucible Timeline",
        string unitName = "Tick")
    {
        return new SorophyTimeSchema(
            timeline,
            [
                new SorophyTimeUnit(
                    unitName,
                    0,
                    new SorophyTimePositionDefinition(SorophyTimePositionKind.Numeric))
            ]);
    }

    public static SorophyTime CreateTime(
        SorophyTimeSchema schema,
        long position,
        string unitName = "Tick")
    {
        return new SorophyTime(
            schema,
            position.ToString(),
            unitName,
            SorophyTimePrecision.Exact);
    }

    public static SorophyTime CreateTime(
        SorophyTimeSchema schema,
        string position,
        string unitName = "Tick")
    {
        return new SorophyTime(
            schema,
            position,
            unitName,
            SorophyTimePrecision.Exact);
    }

    public static SorophyGraph CreateGraphWithEndpoints(
        out Guid sourceId,
        out Guid targetId)
    {
        var graph = new SorophyGraph();

        sourceId = Guid.NewGuid();
        targetId = Guid.NewGuid();

        graph.AddEntity(new SorophyEntity
        {
            Id = sourceId,
            Name = "Source Entity",
            Type = "Location"
        });

        graph.AddEntity(new SorophyEntity
        {
            Id = targetId,
            Name = "Target Entity",
            Type = "Location"
        });

        return graph;
    }

    public static List<Guid> AddEntities(
        SorophyGraph graph,
        int count,
        string type = "Character")
    {
        var ids = new List<Guid>(count);

        for (int i = 0; i < count; i++)
        {
            var id = Guid.NewGuid();
            graph.AddEntity(new SorophyEntity
            {
                Id = id,
                Name = $"{type} {i + 1}",
                Type = type
            });
            ids.Add(id);
        }

        return ids;
    }

    public static SorophyEntity CreateEventEntity(
        SorophyGraph graph,
        SorophyTime occurredAt,
        string name = "Crucible Event")
    {
        var eventId = Guid.NewGuid();
        var entity = new SorophyEntity
        {
            Id = eventId,
            Name = name,
            Type = "Event",
            OccurredAt = occurredAt
        };

        graph.AddEntity(entity);
        return entity;
    }
}

