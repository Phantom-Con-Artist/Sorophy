using Orb.Engine.Types;

namespace Orb.Engine.Graph;

public sealed class OrbEntity
{
    // Stable identity
    public Guid Id { get; set; } = Guid.NewGuid();

    // Human-readable name
    public string? Name { get; set; }

    // Classification
    public string? Type { get; set; }

    // Entity properties
    public Dictionary<string, OrbProperty> Properties { get; set; } = new();
}