using Content.Server.NPC.Components;

namespace Content.Server.NPC.Systems;

/// <summary>
/// Prevents an NPC from attacking some entities from an enemy faction.
/// </summary>
public sealed class FactionExceptionSystem : EntitySystem
{
    /// <summary>
    /// Returns whether the entity from an enemy faction won't be attacked
    /// </summary>
    public bool IsIgnored(FactionExceptionComponent comp, EntityUid target)
    {
        return comp.Ignored.Contains(target);
    }

    /// <summary>
    /// Prevents an entity from an enemy faction from being attacked
    /// </summary>
    public void IgnoreEntity(FactionExceptionComponent comp, EntityUid target)
    {
        comp.Ignored.Add(target);
    }

    /// <summary>
    /// Prevents a list of entities from an enemy faction from being attacked
    /// </summary>
    public void IgnoreEntities(FactionExceptionComponent comp, IEnumerable<EntityUid> ignored)
    {
        comp.Ignored.UnionWith(ignored);
    }
}
