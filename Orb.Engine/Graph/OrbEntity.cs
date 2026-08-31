namespace Orb.Engine.Graph;

public sealed class OrbEntity
{
    // Stable identity
    public Guid Id { get; set; } = Guid.NewGuid();

    // Human-readable name
    public string? Name { get; set; }

    // Optional classification
    public string? Type { get; set; }

    // User-defined properties
    public Dictionary<string, OrbProperty> Properties { get; } = new();
}