using Orb.Engine.Graph;
using Orb.Engine.Serialization;

namespace Orb.Engine.Storage;

public static class LoreStorage
{
    public static void Save(
        OrbGraph graph,
        string filePath)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var json = LoreSerializer.Serialize(graph);

        File.WriteAllText(filePath, json);
    }

    public static OrbGraph Load(
        string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                "The lore file could not be found.",
                filePath);
        }

        var json = File.ReadAllText(filePath);

        return LoreSerializer.Deserialize(json);
    }
}