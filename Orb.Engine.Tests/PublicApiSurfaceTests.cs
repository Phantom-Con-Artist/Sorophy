using System.Reflection;
using Orb.Engine.Graph;
using Orb.Engine.Serialization;
using Orb.Engine.Storage;
using Orb.Engine.Types;

namespace Orb.Engine.Tests;

public sealed class PublicApiSurfaceTests
{
    [Fact]
    public void IntendedPublicTypes_ShouldBePublic()
    {
        var intendedPublicTypes = new[]
        {
            typeof(OrbGraph),
            typeof(OrbEntity),
            typeof(OrbRelationship),
            typeof(OrbProperty),
            typeof(OrbValue),
            typeof(OrbValueType),
            typeof(EntitySerializer),
            typeof(LoreSerializer),
            typeof(EntityStorage),
            typeof(LoreStorage)
        };

        foreach (var type in intendedPublicTypes)
        {
            Assert.True(
                type.IsPublic,
                $"Expected '{type.FullName}' to be public.");
        }
    }

    [Fact]
    public void SerializationImplementationTypes_ShouldNotBePublic()
    {
        var assembly =
            typeof(EntitySerializer).Assembly;

        var leakedTypeNames = new[]
        {
            "EntityDocument",
            "EntityPropertyDocument",
            "LoreDocument",
            "LoreEntityDocument",
            "LoreRelationshipDocument",
            "OrbValueCodec"
        };

        foreach (var typeName in leakedTypeNames)
        {
            var type =
                assembly.GetType(
                    $"Orb.Engine.Serialization.{typeName}");

            if (type is null)
            {
                continue;
            }

            Assert.False(
                type.IsPublic,
                $"Implementation type '{type.FullName}' must not be public.");
        }
    }

    [Fact]
    public void OrbValueCodec_ShouldBeInternal()
    {
        var assembly =
            typeof(EntitySerializer).Assembly;

        var codec =
            assembly.GetType(
                "Orb.Engine.Serialization.OrbValueCodec");

        Assert.NotNull(codec);
        Assert.False(
            codec!.IsPublic,
            "OrbValueCodec must remain internal.");
    }

    [Fact]
    public void PublicApi_ShouldNotExposeSerializationImplementationTypes()
    {
        var assembly =
            typeof(EntitySerializer).Assembly;

        var forbiddenNames = new HashSet<string>
        {
            "EntityDocument",
            "EntityPropertyDocument",
            "LoreDocument",
            "LoreEntityDocument",
            "LoreRelationshipDocument",
            "OrbValueCodec"
        };

        var publicTypes =
            assembly
                .GetExportedTypes()
                .Where(type =>
                    type.Namespace is not null &&
                    type.Namespace.StartsWith(
                        "Orb.Engine",
                        StringComparison.Ordinal))
                .ToArray();

        foreach (var type in publicTypes)
        {
            Assert.DoesNotContain(
                type.Name,
                forbiddenNames);
        }
    }
}