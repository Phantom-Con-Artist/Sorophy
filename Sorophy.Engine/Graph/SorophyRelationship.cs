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


using System;
using System.Collections.Generic;

namespace Sorophy.Engine.Graph;

public sealed class SorophyRelationship
{
    // Stable identity
    public Guid Id { get; set; } = Guid.NewGuid();

    // Semantic type of the relationship
    public string Type { get; set; } = string.Empty;

    // Entity from which the relationship originates
    public Guid SourceId { get; set; }

    // Entity toward which the relationship points
    public Guid TargetId { get; set; }

    // Properties describing the relationship itself
    public Dictionary<string, SorophyProperty> Properties { get; } = new();
}