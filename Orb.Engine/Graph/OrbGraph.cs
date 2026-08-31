namespace Orb.Engine.Graph;

public sealed class OrbGraph
{
    private readonly Dictionary<Guid, OrbEntity> _entities = new();
    private readonly Dictionary<Guid, OrbRelationship> _relationships = new();

    public IReadOnlyDictionary<Guid, OrbEntity> Entities => _entities;

    public IReadOnlyDictionary<Guid, OrbRelationship> Relationships => _relationships;

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

        // Remove relationships connected to the deleted entity.
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

    public bool TryGetEntity(Guid entityId, out OrbEntity? entity)
    {
        return _entities.TryGetValue(entityId, out entity);
    }

    public bool TryGetRelationship(
        Guid relationshipId,
        out OrbRelationship? relationship)
    {
        return _relationships.TryGetValue(relationshipId, out relationship);
    }
}