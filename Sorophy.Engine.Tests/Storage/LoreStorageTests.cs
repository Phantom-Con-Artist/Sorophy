using Sorophy.Engine.Graph;
using Sorophy.Engine.Storage;
using Sorophy.Engine.Types;

namespace Sorophy.Engine.Tests.Storage;

public class LoreStorageTests
{
    [Fact]
    public void Save_ShouldCreateLoreFile()
    {
        var graph = new SorophyGraph();

        var entity = new SorophyEntity
        {
            Name = "Avaria",
            Type = "Kingdom"
        };

        graph.AddEntity(entity);

        var filePath = CreateTemporaryFilePath();

        try
        {
            LoreStorage.Save(graph, filePath);

            Assert.True(File.Exists(filePath));
        }
        finally
        {
            DeleteIfExists(filePath);
        }
    }

    [Fact]
    public void Load_ShouldRestoreEntities()
    {
        var graph = new SorophyGraph();

        var entity = new SorophyEntity
        {
            Name = "Avaria",
            Type = "Kingdom"
        };

        graph.AddEntity(entity);

        var filePath = CreateTemporaryFilePath();

        try
        {
            LoreStorage.Save(graph, filePath);

            var restored = LoreStorage.Load(filePath);

            Assert.Single(restored.Entities);

            var restoredEntity =
                restored.Entities[entity.Id];

            Assert.Equal(entity.Id, restoredEntity.Id);
            Assert.Equal(entity.Name, restoredEntity.Name);
            Assert.Equal(entity.Type, restoredEntity.Type);
        }
        finally
        {
            DeleteIfExists(filePath);
        }
    }

    [Fact]
    public void SaveAndLoad_ShouldPreserveEntityProperties()
    {
        var graph = new SorophyGraph();

        var entity = new SorophyEntity
        {
            Name = "Avaria",
            Type = "Kingdom"
        };

        entity.Properties["population"] = new SorophyProperty
        {
            Name = "population",
            Value = new SorophyValue(
                SorophyValueType.Integer,
                2400000L)
        };

        entity.Properties["language"] = new SorophyProperty
        {
            Name = "language",
            Value = new SorophyValue(
                SorophyValueType.String,
                "Avarian")
        };

        graph.AddEntity(entity);

        var filePath = CreateTemporaryFilePath();

        try
        {
            LoreStorage.Save(graph, filePath);

            var restored = LoreStorage.Load(filePath);

            var restoredEntity =
                restored.Entities[entity.Id];

            Assert.Equal(
                2,
                restoredEntity.Properties.Count);

            Assert.Equal(
                2400000L,
                restoredEntity.Properties["population"]
                    .Value.Value);

            Assert.Equal(
                "Avarian",
                restoredEntity.Properties["language"]
                    .Value.Value);
        }
        finally
        {
            DeleteIfExists(filePath);
        }
    }

    [Fact]
    public void Load_ShouldRestoreRelationships()
    {
        var graph = new SorophyGraph();

        var source = new SorophyEntity
        {
            Name = "Avaria",
            Type = "Kingdom"
        };

        var target = new SorophyEntity
        {
            Name = "Valor",
            Type = "City"
        };

        graph.AddEntity(source);
        graph.AddEntity(target);

        var relationship = new SorophyRelationship
        {
            Type = "capital_of",
            SourceId = source.Id,
            TargetId = target.Id
        };

        graph.AddRelationship(relationship);

        var filePath = CreateTemporaryFilePath();

        try
        {
            LoreStorage.Save(graph, filePath);

            var restored = LoreStorage.Load(filePath);

            Assert.Single(restored.Relationships);

            var restoredRelationship =
                restored.Relationships[relationship.Id];

            Assert.Equal(
                relationship.Id,
                restoredRelationship.Id);

            Assert.Equal(
                relationship.Type,
                restoredRelationship.Type);

            Assert.Equal(
                source.Id,
                restoredRelationship.SourceId);

            Assert.Equal(
                target.Id,
                restoredRelationship.TargetId);
        }
        finally
        {
            DeleteIfExists(filePath);
        }
    }

    [Fact]
    public void SaveAndLoad_ShouldPreserveRelationshipProperties()
    {
        var graph = new SorophyGraph();

        var source = new SorophyEntity
        {
            Name = "Avaria"
        };

        var target = new SorophyEntity
        {
            Name = "Valor"
        };

        graph.AddEntity(source);
        graph.AddEntity(target);

        var relationship = new SorophyRelationship
        {
            Type = "ruled_by",
            SourceId = source.Id,
            TargetId = target.Id
        };

        relationship.Properties["since"] =
            new SorophyProperty
            {
                Name = "since",
                Value = new SorophyValue(
                    SorophyValueType.Integer,
                    482L)
            };

        graph.AddRelationship(relationship);

        var filePath = CreateTemporaryFilePath();

        try
        {
            LoreStorage.Save(graph, filePath);

            var restored = LoreStorage.Load(filePath);

            var restoredRelationship =
                restored.Relationships[relationship.Id];

            Assert.Equal(
                482L,
                restoredRelationship
                    .Properties["since"]
                    .Value.Value);
        }
        finally
        {
            DeleteIfExists(filePath);
        }
    }

    [Fact]
    public void SelfRelationship_ShouldSurviveDiskRoundTrip()
    {
        var graph = new SorophyGraph();

        var entity = new SorophyEntity
        {
            Name = "Avaria"
        };

        graph.AddEntity(entity);

        var relationship = new SorophyRelationship
        {
            Type = "references",
            SourceId = entity.Id,
            TargetId = entity.Id
        };

        graph.AddRelationship(relationship);

        var filePath = CreateTemporaryFilePath();

        try
        {
            LoreStorage.Save(graph, filePath);

            var restored = LoreStorage.Load(filePath);

            Assert.Single(restored.Relationships);

            var restoredRelationship =
                restored.Relationships[relationship.Id];

            Assert.Equal(
                restoredRelationship.SourceId,
                restoredRelationship.TargetId);

            Assert.Equal(
                entity.Id,
                restoredRelationship.SourceId);
        }
        finally
        {
            DeleteIfExists(filePath);
        }
    }

    [Fact]
    public void MultipleRelationships_ShouldSurviveDiskRoundTrip()
    {
        var graph = new SorophyGraph();

        var source = new SorophyEntity
        {
            Name = "A"
        };

        var target = new SorophyEntity
        {
            Name = "B"
        };

        graph.AddEntity(source);
        graph.AddEntity(target);

        var first = new SorophyRelationship
        {
            Type = "knows",
            SourceId = source.Id,
            TargetId = target.Id
        };

        var second = new SorophyRelationship
        {
            Type = "knows",
            SourceId = source.Id,
            TargetId = target.Id
        };

        graph.AddRelationship(first);
        graph.AddRelationship(second);

        var filePath = CreateTemporaryFilePath();

        try
        {
            LoreStorage.Save(graph, filePath);

            var restored = LoreStorage.Load(filePath);

            Assert.Equal(
                2,
                restored.Relationships.Count);

            Assert.Contains(
                first.Id,
                restored.Relationships.Keys);

            Assert.Contains(
                second.Id,
                restored.Relationships.Keys);
        }
        finally
        {
            DeleteIfExists(filePath);
        }
    }

    [Fact]
    public void ReverseRelationships_ShouldSurviveDiskRoundTrip()
    {
        var graph = new SorophyGraph();

        var first = new SorophyEntity
        {
            Name = "A"
        };

        var second = new SorophyEntity
        {
            Name = "B"
        };

        graph.AddEntity(first);
        graph.AddEntity(second);

        var forward = new SorophyRelationship
        {
            Type = "knows",
            SourceId = first.Id,
            TargetId = second.Id
        };

        var reverse = new SorophyRelationship
        {
            Type = "knows",
            SourceId = second.Id,
            TargetId = first.Id
        };

        graph.AddRelationship(forward);
        graph.AddRelationship(reverse);

        var filePath = CreateTemporaryFilePath();

        try
        {
            LoreStorage.Save(graph, filePath);

            var restored = LoreStorage.Load(filePath);

            Assert.Equal(
                2,
                restored.Relationships.Count);

            var restoredForward =
                restored.Relationships[forward.Id];

            var restoredReverse =
                restored.Relationships[reverse.Id];

            Assert.Equal(
                first.Id,
                restoredForward.SourceId);

            Assert.Equal(
                second.Id,
                restoredForward.TargetId);

            Assert.Equal(
                second.Id,
                restoredReverse.SourceId);

            Assert.Equal(
                first.Id,
                restoredReverse.TargetId);
        }
        finally
        {
            DeleteIfExists(filePath);
        }
    }

    [Fact]
    public void EmptyGraph_ShouldSurviveDiskRoundTrip()
    {
        var graph = new SorophyGraph();

        var filePath = CreateTemporaryFilePath();

        try
        {
            LoreStorage.Save(graph, filePath);

            var restored = LoreStorage.Load(filePath);

            Assert.Empty(restored.Entities);
            Assert.Empty(restored.Relationships);
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
            LoreStorage.Load(filePath));
    }

    [Fact]
    public void Load_ShouldRejectInvalidLoreFile()
    {
        var filePath = CreateTemporaryFilePath();

        try
        {
            File.WriteAllText(
                filePath,
                "{ this is invalid json");

            Assert.ThrowsAny<Exception>(() =>
                LoreStorage.Load(filePath));
        }
        finally
        {
            DeleteIfExists(filePath);
        }
    }

    [Fact]
    public void CompleteGraph_ShouldSurviveDiskRoundTrip()
    {
        var graph = new SorophyGraph();

        var avaria = new SorophyEntity
        {
            Name = "Avaria",
            Type = "Kingdom"
        };

        avaria.Properties["population"] =
            new SorophyProperty
            {
                Name = "population",
                Value = new SorophyValue(
                    SorophyValueType.Integer,
                    2400000L)
            };

        var valor = new SorophyEntity
        {
            Name = "Valor",
            Type = "City"
        };

        valor.Properties["language"] =
            new SorophyProperty
            {
                Name = "language",
                Value = new SorophyValue(
                    SorophyValueType.String,
                    "Avarian")
            };

        graph.AddEntity(avaria);
        graph.AddEntity(valor);

        var relationship = new SorophyRelationship
        {
            Type = "capital_of",
            SourceId = avaria.Id,
            TargetId = valor.Id
        };

        relationship.Properties["since"] =
            new SorophyProperty
            {
                Name = "since",
                Value = new SorophyValue(
                    SorophyValueType.Integer,
                    482L)
            };

        graph.AddRelationship(relationship);

        var filePath = CreateTemporaryFilePath();

        try
        {
            LoreStorage.Save(graph, filePath);

            var restored = LoreStorage.Load(filePath);

            Assert.Equal(
                graph.Entities.Count,
                restored.Entities.Count);

            Assert.Equal(
                graph.Relationships.Count,
                restored.Relationships.Count);

            Assert.Equal(
                avaria.Name,
                restored.Entities[avaria.Id].Name);

            Assert.Equal(
                valor.Name,
                restored.Entities[valor.Id].Name);

            Assert.Equal(
                "Avarian",
                restored.Entities[valor.Id]
                    .Properties["language"]
                    .Value.Value);

            var restoredRelationship =
                restored.Relationships[relationship.Id];

            Assert.Equal(
                relationship.SourceId,
                restoredRelationship.SourceId);

            Assert.Equal(
                relationship.TargetId,
                restoredRelationship.TargetId);

            Assert.Equal(
                482L,
                restoredRelationship
                    .Properties["since"]
                    .Value.Value);
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
            $"sorophy-lore-{Guid.NewGuid():N}.lore");
    }

    private static void DeleteIfExists(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
}