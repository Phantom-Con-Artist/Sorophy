using Orb.Engine.Graph;
using Orb.Engine.Serialization;

namespace Orb.Engine.Storage;

public static class EntityStorage
{
    public static void Save(
        OrbEntity entity,
        string filePath)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var json = EntitySerializer.Serialize(entity);

        File.WriteAllText(filePath, json);
    }

    public static OrbEntity Load(
        string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                "The entity file could not be found.",
                filePath);
        }

        var json = File.ReadAllText(filePath);

        return EntitySerializer.Deserialize(json);
    }
}