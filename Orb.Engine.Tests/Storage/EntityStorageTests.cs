using Orb.Engine.Graph;
using Orb.Engine.Storage;
using Orb.Engine.Types;

namespace Orb.Engine.Tests.Storage;

public class EntityStorageTests
{
    [Fact]
    public void Save_ShouldCreateEntityFile()
    {
        var entity = new OrbEntity
        {
            Name = "Avaria",
            Type = "Kingdom"
        };

        var filePath = CreateTemporaryFilePath();

        try
        {
            EntityStorage.Save(entity, filePath);

            Assert.True(File.Exists(filePath));
        }
        finally
        {
            DeleteIfExists(filePath);
        }
    }

    [Fact]
    public void Load_ShouldRestoreEntity()
    {
        var entity = new OrbEntity
        {
            Name = "Avaria",
            Type = "Kingdom"
        };

        var filePath = CreateTemporaryFilePath();

        try
        {
            EntityStorage.Save(entity, filePath);

            var restored = EntityStorage.Load(filePath);

            Assert.Equal(entity.Id, restored.Id);
            Assert.Equal(entity.Name, restored.Name);
            Assert.Equal(entity.Type, restored.Type);
        }
        finally
        {
            DeleteIfExists(filePath);
        }
    }

    [Fact]
    public void SaveAndLoad_ShouldPreserveProperties()
    {
        var entity = new OrbEntity
        {
            Name = "Avaria",
            Type = "Kingdom"
        };

        entity.Properties["population"] = new OrbProperty
        {
            Name = "population",
            Value = new OrbValue(
                OrbValueType.Integer,
                2400000L)
        };

        entity.Properties["language"] = new OrbProperty
        {
            Name = "language",
            Value = new OrbValue(
                OrbValueType.String,
                "Avarian")
        };

        var filePath = CreateTemporaryFilePath();

        try
        {
            EntityStorage.Save(entity, filePath);

            var restored = EntityStorage.Load(filePath);

            Assert.Equal(
                2,
                restored.Properties.Count);

            Assert.Equal(
                2400000L,
                restored.Properties["population"]
                    .Value.Value);

            Assert.Equal(
                "Avarian",
                restored.Properties["language"]
                    .Value.Value);
        }
        finally
        {
            DeleteIfExists(filePath);
        }
    }

    [Fact]
    public void SaveAndLoad_ShouldPreserveNullProperty()
    {
        var entity = new OrbEntity
        {
            Name = "Avaria"
        };

        entity.Properties["unknown"] = new OrbProperty
        {
            Name = "unknown",
            Value = new OrbValue(
                OrbValueType.Null,
                null)
        };

        var filePath = CreateTemporaryFilePath();

        try
        {
            EntityStorage.Save(entity, filePath);

            var restored = EntityStorage.Load(filePath);

            var property =
                restored.Properties["unknown"];

            Assert.Equal(
                OrbValueType.Null,
                property.Value.Type);

            Assert.Null(property.Value.Value);
        }
        finally
        {
            DeleteIfExists(filePath);
        }
    }

    [Fact]
    public void Load_ShouldRejectMissingFile()
    {
        var filePath = CreateTemporaryFilePath();

        Assert.Throws<FileNotFoundException>(() =>
            EntityStorage.Load(filePath));
    }

    [Fact]
    public void Load_ShouldRejectInvalidEntityFile()
    {
        var filePath = CreateTemporaryFilePath();

        try
        {
            File.WriteAllText(
                filePath,
                "{ this is invalid json");

            Assert.ThrowsAny<Exception>(() =>
                EntityStorage.Load(filePath));
        }
        finally
        {
            DeleteIfExists(filePath);
        }
    }

    [Fact]
    public void SaveAndLoad_ShouldPreserveIdentity()
    {
        var id = Guid.NewGuid();

        var entity = new OrbEntity
        {
            Id = id,
            Name = "Avaria"
        };

        var filePath = CreateTemporaryFilePath();

        try
        {
            EntityStorage.Save(entity, filePath);

            var restored = EntityStorage.Load(filePath);

            Assert.Equal(id, restored.Id);
        }
        finally
        {
            DeleteIfExists(filePath);
        }
    }

    [Fact]
    public void SaveAndLoad_ShouldPreserveEmptyEntity()
    {
        var entity = new OrbEntity();

        var filePath = CreateTemporaryFilePath();

        try
        {
            EntityStorage.Save(entity, filePath);

            var restored = EntityStorage.Load(filePath);

            Assert.Equal(entity.Id, restored.Id);
            Assert.Empty(restored.Properties);
        }
        finally
        {
            DeleteIfExists(filePath);
        }
    }

    private static string CreateTemporaryFilePath()
    {
        return Path.Combine(
            Path.GetTempPath(),
            $"orb-entity-{Guid.NewGuid():N}.entity");
    }

    private static void DeleteIfExists(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
}
