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