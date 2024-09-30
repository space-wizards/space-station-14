using Content.Shared.NPC.Components;
using System.Linq;

namespace Content.Shared.NPC.Systems;

/// <summary>
/// Prevents an NPC from attacking some entities from an enemy faction.
/// Also makes it attack some entities even if they are in neutral factions (retaliation).
/// </summary>
public sealed partial class NpcFactionSystem
{
    private EntityQuery<FactionExceptionComponent> _exceptionQuery;
    private EntityQuery<FactionExceptionTrackerComponent> _trackerQuery;

    public void InitializeException()
    {
        _exceptionQuery = GetEntityQuery<FactionExceptionComponent>();
        _trackerQuery = GetEntityQuery<FactionExceptionTrackerComponent>();

        SubscribeLocalEvent<FactionExceptionComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<FactionExceptionTrackerComponent, ComponentShutdown>(OnTrackerShutdown);
    }

    private void OnShutdown(Entity<FactionExceptionComponent> ent, ref ComponentShutdown args)
    {
        foreach (var uid in ent.Comp.Hostiles)
        {
            if (_trackerQuery.TryGetComponent(uid, out var tracker))
                tracker.Entities.Remove(ent);
        }

        foreach (var uid in ent.Comp.Ignored)
        {
            if (_trackerQuery.TryGetComponent(uid, out var tracker))
                tracker.Entities.Remove(ent);
        }
    }

    private void OnTrackerShutdown(Entity<FactionExceptionTrackerComponent> ent, ref ComponentShutdown args)
    {
        foreach (var uid in ent.Comp.Entities)
        {
            if (!_exceptionQuery.TryGetComponent(uid, out var exception))
                continue;

            exception.Ignored.Remove(ent);
            exception.Hostiles.Remove(ent);
        }
    }

    /// <summary>
    /// Returns whether the entity from an enemy faction won't be attacked
    /// </summary>
    public bool IsIgnored(Entity<FactionExceptionComponent?> ent, EntityUid target)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        return ent.Comp.Ignored.Contains(target);
    }

    /// <summary>
    /// Returns the specific hostile entities for a given entity.
    /// </summary>
    public IEnumerable<EntityUid> GetHostiles(Entity<FactionExceptionComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return Array.Empty<EntityUid>();

        // evil c#
        return ent.Comp!.Hostiles;
    }

    /// <summary>
    /// Prevents an entity from an enemy faction from being attacked
    /// </summary>
    public void IgnoreEntity(Entity<FactionExceptionComponent?> ent, Entity<FactionExceptionTrackerComponent?> target)
    {
        ent.Comp ??= EnsureComp<FactionExceptionComponent>(ent);
        ent.Comp.Ignored.Add(target);
        target.Comp ??= EnsureComp<FactionExceptionTrackerComponent>(target);
        target.Comp.Entities.Add(ent);
    }

    /// <summary>
    /// Prevents a list of entities from an enemy faction from being attacked
    /// </summary>
    public void IgnoreEntities(Entity<FactionExceptionComponent?> ent, IEnumerable<EntityUid> ignored)
    {
        ent.Comp ??= EnsureComp<FactionExceptionComponent>(ent);
        foreach (var ignore in ignored)
        {
            IgnoreEntity(ent, ignore);
        }
    }

    /// <summary>
    /// Makes an entity always be considered hostile.
    /// </summary>
    public void AggroEntity(Entity<FactionExceptionComponent?> ent, Entity<FactionExceptionTrackerComponent?> target)
    {
        ent.Comp ??= EnsureComp<FactionExceptionComponent>(ent);
        ent.Comp.Hostiles.Add(target);
        target.Comp ??= EnsureComp<FactionExceptionTrackerComponent>(target);
        target.Comp.Entities.Add(ent);
    }

    /// <summary>
    /// Makes an entity no longer be considered hostile, if it was.
    /// Doesn't apply to regular faction hostilities.
    /// </summary>
    public void DeAggroEntity(Entity<FactionExceptionComponent?> ent, EntityUid target)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (!ent.Comp.Hostiles.Remove(target) || !_trackerQuery.TryGetComponent(target, out var tracker))
            return;

        tracker.Entities.Remove(ent);
    }

    /// <summary>
    /// Makes a list of entities no longer be considered hostile, if it was.
    /// Doesn't apply to regular faction hostilities.
    /// </summary>
    public void AggroEntities(Entity<FactionExceptionComponent?> ent, IEnumerable<EntityUid> entities)
    {
        ent.Comp ??= EnsureComp<FactionExceptionComponent>(ent);
        foreach (var uid in entities)
        {
            AggroEntity(ent, uid);
        }
    }
}
