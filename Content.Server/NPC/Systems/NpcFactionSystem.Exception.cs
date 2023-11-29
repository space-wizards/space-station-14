using System.Linq;
using Content.Server.NPC.Components;

namespace Content.Server.NPC.Systems;

/// <summary>
/// Prevents an NPC from attacking some entities from an enemy faction.
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

    private void OnShutdown(EntityUid uid, FactionExceptionComponent component, ComponentShutdown args)
    {
        foreach (var ent in component.Hostiles)
        {
            if (!_trackerQuery.TryGetComponent(ent, out var trackerComponent))
                continue;
            trackerComponent.Entities.Remove(uid);
        }

        foreach (var ent in component.Ignored)
        {
            if (!_trackerQuery.TryGetComponent(ent, out var trackerComponent))
                continue;
            trackerComponent.Entities.Remove(uid);
        }
    }

    private void OnTrackerShutdown(EntityUid uid, FactionExceptionTrackerComponent component, ComponentShutdown args)
    {
        foreach (var ent in component.Entities)
        {
            if (!_exceptionQuery.TryGetComponent(ent, out var exceptionComponent))
                continue;
            exceptionComponent.Ignored.Remove(uid);
            exceptionComponent.Hostiles.Remove(uid);
        }
    }

    /// <summary>
    /// Returns whether the entity from an enemy faction won't be attacked
    /// </summary>
    public bool IsIgnored(EntityUid uid, EntityUid target, FactionExceptionComponent? comp = null)
    {
        if (!Resolve(uid, ref comp, false))
            return false;

        return comp.Ignored.Contains(target);
    }

    /// <summary>
    /// Returns the specific hostile entities for a given entity.
    /// </summary>
    public IEnumerable<EntityUid> GetHostiles(EntityUid uid, FactionExceptionComponent? comp = null)
    {
        if (!Resolve(uid, ref comp, false))
            return Array.Empty<EntityUid>();

        return comp.Hostiles;
    }

    /// <summary>
    /// Prevents an entity from an enemy faction from being attacked
    /// </summary>
    public void IgnoreEntity(EntityUid uid, EntityUid target, FactionExceptionComponent? comp = null)
    {
        comp ??= EnsureComp<FactionExceptionComponent>(uid);
        comp.Ignored.Add(target);
        EnsureComp<FactionExceptionTrackerComponent>(target).Entities.Add(uid);
    }

    /// <summary>
    /// Prevents a list of entities from an enemy faction from being attacked
    /// </summary>
    public void IgnoreEntities(EntityUid uid, IEnumerable<EntityUid> ignored, FactionExceptionComponent? comp = null)
    {
        comp ??= EnsureComp<FactionExceptionComponent>(uid);
        foreach (var ignore in ignored)
        {
            IgnoreEntity(uid, ignore, comp);
        }
    }

    /// <summary>
    /// Makes an entity always be considered hostile.
    /// </summary>
    public void AggroEntity(EntityUid uid, EntityUid target, FactionExceptionComponent? comp = null)
    {
        comp ??= EnsureComp<FactionExceptionComponent>(uid);
        comp.Hostiles.Add(target);
        EnsureComp<FactionExceptionTrackerComponent>(target).Entities.Add(uid);
    }

    /// <summary>
    /// Makes an entity no longer be considered hostile, if it was.
    /// Doesn't apply to regular faction hostilities.
    /// </summary>
    public void DeAggroEntity(EntityUid uid, EntityUid target, FactionExceptionComponent? comp = null)
    {
        if (!Resolve(uid, ref comp, false))
            return;
        if (!comp.Hostiles.Remove(target) || !_trackerQuery.TryGetComponent(target, out var tracker))
            return;
        tracker.Entities.Remove(uid);
    }

    /// <summary>
    /// Makes a list of entities no longer be considered hostile, if it was.
    /// Doesn't apply to regular faction hostilities.
    /// </summary>
    public void AggroEntities(EntityUid uid, IEnumerable<EntityUid> entities, FactionExceptionComponent? comp = null)
    {
        comp ??= EnsureComp<FactionExceptionComponent>(uid);
        foreach (var ent in entities)
        {
            AggroEntity(uid, ent, comp);
        }
    }
}
