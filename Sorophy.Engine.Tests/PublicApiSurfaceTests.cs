using System.Reflection;
using Sorophy.Engine.Graph;
using Sorophy.Engine.Serialization;
using Sorophy.Engine.Storage;
using Sorophy.Engine.Types;

namespace Sorophy.Engine.Tests;

public sealed class PublicApiSurfaceTests
{
    [Fact]
    public void IntendedPublicTypes_ShouldBePublic()
    {
        var intendedPublicTypes = new[]
        {
            typeof(SorophyGraph),
            typeof(SorophyEntity),
            typeof(SorophyRelationship),
            typeof(SorophyProperty),
            typeof(SorophyValue),
            typeof(SorophyValueType),
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
            "SorophyValueCodec"
        };

        foreach (var typeName in leakedTypeNames)
        {
            var type =
                assembly.GetType(
                    $"Sorophy.Engine.Serialization.{typeName}");

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
    public void SorophyValueCodec_ShouldBeInternal()
    {
        var assembly =
            typeof(EntitySerializer).Assembly;

        var codec =
            assembly.GetType(
                "Sorophy.Engine.Serialization.SorophyValueCodec");

        Assert.NotNull(codec);
        Assert.False(
            codec!.IsPublic,
            "SorophyValueCodec must remain internal.");
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
            "SorophyValueCodec"
        };

        var publicTypes =
            assembly
                .GetExportedTypes()
                .Where(type =>
                    type.Namespace is not null &&
                    type.Namespace.StartsWith(
                        "Sorophy.Engine",
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