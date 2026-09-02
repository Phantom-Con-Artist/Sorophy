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
using System.Text.Json.Serialization;

namespace Orb.Engine.Serialization;

internal sealed class LoreDocument
{
    [JsonPropertyName("formatVersion")]
    public int? FormatVersion { get; set; }

    [JsonPropertyName("entities")]
    public List<LoreEntityDocument> Entities { get; set; } = new();

    [JsonPropertyName("relationships")]
    public List<LoreRelationshipDocument> Relationships { get; set; } = new();
}

internal sealed class LoreEntityDocument
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("properties")]
    public Dictionary<string, EntityPropertyDocument> Properties { get; set; } = new();
}

internal sealed class LoreRelationshipDocument
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("sourceId")]
    public Guid SourceId { get; set; }

    [JsonPropertyName("targetId")]
    public Guid TargetId { get; set; }

    [JsonPropertyName("properties")]
    public Dictionary<string, EntityPropertyDocument> Properties { get; set; } = new();
}