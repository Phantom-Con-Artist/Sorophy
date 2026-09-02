/*
Orb Engine — a structured knowledge and graph engine
Copyright (C) 2026  Subhradeep Sarkar

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published
by the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.

*/


using System.Collections.ObjectModel;

namespace Orb.Engine.Graph;

public sealed class OrbGraph
{
    private readonly Dictionary<Guid, OrbEntity> _entities = new();
    private readonly Dictionary<Guid, OrbRelationship> _relationships = new();

    private readonly ReadOnlyDictionary<Guid, OrbEntity> _readOnlyEntities;
    private readonly ReadOnlyDictionary<Guid, OrbRelationship> _readOnlyRelationships;

    public OrbGraph()
    {
        _readOnlyEntities =
            new ReadOnlyDictionary<Guid, OrbEntity>(_entities);

        _readOnlyRelationships =
            new ReadOnlyDictionary<Guid, OrbRelationship>(_relationships);
    }

    public IReadOnlyDictionary<Guid, OrbEntity> Entities =>
        _readOnlyEntities;

    public IReadOnlyDictionary<Guid, OrbRelationship> Relationships =>
        _readOnlyRelationships;

    public void AddEntity(OrbEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (!_entities.TryAdd(entity.Id, entity))
        {
            throw new InvalidOperationException(
                $"An entity with ID '{entity.Id}' already exists.");
        }
    }

    public void AddRelationship(OrbRelationship relationship)
    {
        ArgumentNullException.ThrowIfNull(relationship);

        if (!_entities.ContainsKey(relationship.SourceId))
        {
            throw new InvalidOperationException(
                $"Source entity '{relationship.SourceId}' does not exist.");
        }

        if (!_entities.ContainsKey(relationship.TargetId))
        {
            throw new InvalidOperationException(
                $"Target entity '{relationship.TargetId}' does not exist.");
        }

        if (!_relationships.TryAdd(relationship.Id, relationship))
        {
            throw new InvalidOperationException(
                $"A relationship with ID '{relationship.Id}' already exists.");
        }
    }

    public bool RemoveEntity(Guid entityId)
    {
        if (!_entities.Remove(entityId))
        {
            return false;
        }

        var relationshipsToRemove = _relationships
            .Where(pair =>
                pair.Value.SourceId == entityId ||
                pair.Value.TargetId == entityId)
            .Select(pair => pair.Key)
            .ToList();

        foreach (var relationshipId in relationshipsToRemove)
        {
            _relationships.Remove(relationshipId);
        }

        return true;
    }

    public bool RemoveRelationship(Guid relationshipId)
    {
        return _relationships.Remove(relationshipId);
    }

    public bool ContainsEntity(Guid entityId)
    {
        return _entities.ContainsKey(entityId);
    }

    public bool ContainsRelationship(Guid relationshipId)
    {
        return _relationships.ContainsKey(relationshipId);
    }

    public bool TryGetEntity(
        Guid entityId,
        out OrbEntity? entity)
    {
        return _entities.TryGetValue(
            entityId,
            out entity);
    }

    public bool TryGetRelationship(
        Guid relationshipId,
        out OrbRelationship? relationship)
    {
        return _relationships.TryGetValue(
            relationshipId,
            out relationship);
    }

    public IEnumerable<OrbRelationship> GetOutgoingRelationships(
        Guid entityId)
    {
        return _relationships.Values
            .Where(relationship =>
                relationship.SourceId == entityId);
    }

    public IEnumerable<OrbRelationship> GetIncomingRelationships(
        Guid entityId)
    {
        return _relationships.Values
            .Where(relationship =>
                relationship.TargetId == entityId);
    }

    public IEnumerable<OrbRelationship> GetRelationships(
        Guid entityId)
    {
        return _relationships.Values
            .Where(relationship =>
                relationship.SourceId == entityId ||
                relationship.TargetId == entityId);
    }

    public IEnumerable<OrbEntity> GetNeighbors(
        Guid entityId)
    {
        var neighborIds = new HashSet<Guid>();

        foreach (var relationship in _relationships.Values)
        {
            if (relationship.SourceId == entityId &&
                relationship.TargetId != entityId)
            {
                neighborIds.Add(relationship.TargetId);
            }

            if (relationship.TargetId == entityId &&
                relationship.SourceId != entityId)
            {
                neighborIds.Add(relationship.SourceId);
            }
        }

        foreach (var neighborId in neighborIds)
        {
            if (_entities.TryGetValue(
                    neighborId,
                    out var entity))
            {
                yield return entity;
            }
        }
    }

    public bool IsReachable(
        Guid sourceId,
        Guid targetId)
    {
        if (!ContainsEntity(sourceId) ||
            !ContainsEntity(targetId))
        {
            return false;
        }

        if (sourceId == targetId)
        {
            return true;
        }

        var visited = new HashSet<Guid>();
        var queue = new Queue<Guid>();

        queue.Enqueue(sourceId);
        visited.Add(sourceId);

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();

            foreach (var relationship in GetOutgoingRelationships(currentId))
            {
                var nextId = relationship.TargetId;

                if (nextId == targetId)
                {
                    return true;
                }

                if (visited.Add(nextId))
                {
                    queue.Enqueue(nextId);
                }
            }
        }

        return false;
    }

    public IEnumerable<OrbEntity> Traverse(Guid sourceId)
    {
        if (!ContainsEntity(sourceId))
        {
            yield break;
        }

        var visited = new HashSet<Guid> { sourceId };
        var queue = new Queue<Guid>();

        queue.Enqueue(sourceId);

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();

            foreach (var relationship in GetOutgoingRelationships(currentId))
            {
                var targetId = relationship.TargetId;

                if (!visited.Add(targetId))
                {
                    continue;
                }

                if (_entities.TryGetValue(
                        targetId,
                        out var entity))
                {
                    yield return entity;
                }

                queue.Enqueue(targetId);
            }
        }
    }

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        foreach (var pair in _entities)
        {
            if (pair.Value is null)
            {
                errors.Add(
                    $"Entity dictionary contains a null entity for ID '{pair.Key}'.");
                continue;
            }

            if (pair.Key != pair.Value.Id)
            {
                errors.Add(
                    $"Entity dictionary key '{pair.Key}' does not match entity ID '{pair.Value.Id}'.");
            }

            ValidateProperties(
                pair.Value.Properties,
                $"Entity '{pair.Value.Id}'",
                errors);
        }

        foreach (var pair in _relationships)
        {
            if (pair.Value is null)
            {
                errors.Add(
                    $"Relationship dictionary contains a null relationship for ID '{pair.Key}'.");
                continue;
            }

            var relationship = pair.Value;

            if (pair.Key != relationship.Id)
            {
                errors.Add(
                    $"Relationship dictionary key '{pair.Key}' does not match relationship ID '{relationship.Id}'.");
            }

            if (!_entities.ContainsKey(relationship.SourceId))
            {
                errors.Add(
                    $"Relationship '{relationship.Id}' references missing source entity '{relationship.SourceId}'.");
            }

            if (!_entities.ContainsKey(relationship.TargetId))
            {
                errors.Add(
                    $"Relationship '{relationship.Id}' references missing target entity '{relationship.TargetId}'.");
            }

            ValidateProperties(
                relationship.Properties,
                $"Relationship '{relationship.Id}'",
                errors);
        }

        return errors;
    }

    private static void ValidateProperties(
        IReadOnlyDictionary<string, OrbProperty> properties,
        string ownerDescription,
        List<string> errors)
    {
        foreach (var pair in properties)
        {
            var propertyKey = pair.Key;
            var property = pair.Value;

            if (string.IsNullOrWhiteSpace(propertyKey))
            {
                errors.Add(
                    $"{ownerDescription} contains a property with an empty dictionary key.");
            }

            if (property is null)
            {
                errors.Add(
                    $"{ownerDescription} property '{propertyKey}' is null.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(property.Name))
            {
                errors.Add(
                    $"{ownerDescription} property '{propertyKey}' has an empty name.");
                continue;
            }

            if (!string.Equals(
                    propertyKey,
                    property.Name,
                    StringComparison.Ordinal))
            {
                errors.Add(
                    $"{ownerDescription} property dictionary key '{propertyKey}' does not match property name '{property.Name}'.");
            }
        }
    }
}