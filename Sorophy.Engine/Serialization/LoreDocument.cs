/*
Sorophy Engine — a structured knowledge and graph engine
Copyright (C) 2026  Subhradeep Sarkar

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published
by the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program. If not, see <https://www.gnu.org/licenses/>.
*/

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sorophy.Engine.Serialization;

internal sealed class LoreDocument
{
    [JsonPropertyName("formatVersion")]
    public int? FormatVersion { get; set; }

    [JsonPropertyName("entities")]
    public List<LoreEntityDocument> Entities { get; set; } = new();

    [JsonPropertyName("relationships")]
    public List<LoreRelationshipDocument> Relationships { get; set; } = new();

    [JsonPropertyName("relationshipHistories")]
    public List<LoreRelationshipHistoryDocument>? RelationshipHistories { get; set; }

    [JsonPropertyName("retiredRelationshipIds")]
    public List<Guid>? RetiredRelationshipIds { get; set; }
}

internal sealed class LoreEntityDocument
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }

    [JsonPropertyName("documents")]
    public Dictionary<string, EntityEmbeddedDocument>? Documents { get; set; }

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

    [JsonPropertyName("validFrom")]
    public LoreTimeDocument? ValidFrom { get; set; }

    [JsonPropertyName("validTill")]
    public LoreTimeDocument? ValidTill { get; set; }

    [JsonPropertyName("properties")]
    public Dictionary<string, EntityPropertyDocument> Properties { get; set; } = new();
}

internal sealed class LoreRelationshipHistoryDocument
{
    [JsonPropertyName("relationshipId")]
    public Guid RelationshipId { get; set; }

    [JsonPropertyName("facts")]
    public List<LoreRelationshipFactDocument> Facts { get; set; } = new();
}

internal sealed class LoreRelationshipFactDocument
{
    [JsonPropertyName("at")]
    public LoreTimeDocument? At { get; set; }

    [JsonPropertyName("relationshipId")]
    public Guid RelationshipId { get; set; }

    [JsonPropertyName("sourceId")]
    public Guid SourceId { get; set; }

    [JsonPropertyName("targetId")]
    public Guid TargetId { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("properties")]
    public Dictionary<string, EntityPropertyDocument> Properties { get; set; } = new();

    [JsonPropertyName("validFrom")]
    public LoreTimeDocument? ValidFrom { get; set; }

    [JsonPropertyName("validTill")]
    public LoreTimeDocument? ValidTill { get; set; }

    [JsonPropertyName("eventEntityId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? EventEntityId { get; set; }
}

internal sealed class LoreTimeDocument
{
    [JsonPropertyName("schema")]
    public LoreTimeSchemaDocument Schema { get; set; } = new();

    [JsonPropertyName("position")]
    public string Position { get; set; } = string.Empty;

    [JsonPropertyName("unit")]
    public string Unit { get; set; } = string.Empty;

    [JsonPropertyName("precision")]
    public string Precision { get; set; } = string.Empty;
}

internal sealed class LoreTimeSchemaDocument
{
    [JsonPropertyName("timeline")]
    public string Timeline { get; set; } = string.Empty;

    [JsonPropertyName("units")]
    public List<LoreTimeUnitDocument> Units { get; set; } = new();
}

internal sealed class LoreTimeUnitDocument
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("order")]
    public int Order { get; set; }

    [JsonPropertyName("positionDefinition")]
    public LoreTimePositionDefinitionDocument PositionDefinition { get; set; } = new();
}

internal sealed class LoreTimePositionDefinitionDocument
{
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    [JsonPropertyName("pattern")]
    public string? Pattern { get; set; }
}