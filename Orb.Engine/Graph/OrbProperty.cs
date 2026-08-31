using Orb.Engine.Types;

namespace Orb.Engine.Graph;

public sealed class OrbProperty
{
    public string Name { get; set; } = string.Empty;

    public OrbValue Value { get; set; }
}