namespace Orb.Engine.Graph;

public sealed class OrbRelationship
{
    // Stable identity
    public Guid Id { get; set; } = Guid.NewGuid();

    // Semantic type of the relationship
    public string Type { get; set; } = string.Empty;

    // Entity from which the relationship originates
    public Guid SourceId { get; set; }

    // Entity toward which the relationship points
    public Guid TargetId { get; set; }
}