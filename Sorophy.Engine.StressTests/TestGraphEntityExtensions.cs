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

namespace Sorophy.Engine.Graph;

/// <summary>
/// Test helper extensions allowing existing non-temporal stress tests
/// to construct test entities with a default baseline restoration path.
/// </summary>
internal static class TestGraphEntityExtensions
{
    public static void AddEntity(
        this SorophyGraph graph,
        SorophyEntity entity)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(entity);

        graph.RestoreEntity(entity);
    }
}

