using System.Text.Json;
using System.Text.Json.Serialization;

namespace Orb.Engine.Serialization;

internal sealed class EntityDocument
{
    [JsonPropertyName("formatVersion")]
    public int? FormatVersion { get; set; }

    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("properties")]
    public Dictionary<string, EntityPropertyDocument> Properties { get; set; } = new();
}

internal sealed class EntityPropertyDocument
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public JsonElement Value { get; set; }
}