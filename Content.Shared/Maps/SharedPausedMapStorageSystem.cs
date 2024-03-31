using System.Diagnostics.CodeAnalysis;
using Content.Shared.GameTicking;
using Content.Shared.Maps.Components;
using Robust.Shared.Map;

namespace Content.Shared.Maps;

public sealed class SharedPausedMapStorageSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;

    public EntityUid? PausedMap { get; private set; }

    public override void Initialize()
    {
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent _)
    {
        if (PausedMap == null || !Exists(PausedMap))
            return;

        Del(PausedMap.Value);
        PausedMap = null;
    }

    [MemberNotNullWhen(true, nameof(PausedMap))]
    public bool EnsurePausedMap()
    {
        if (!CheckPausedMap())
        {
            Log.Error("SharedPausedMapStorageSystem failed to ensure a paused map.");
            return false;
        }

        return true;
    }

    [MemberNotNullWhen(true, nameof(PausedMap))]
    private bool CheckPausedMap()
    {
        if (PausedMap != null && Exists(PausedMap))
            return true;

        var id = _mapManager.CreateMap();

        if (id == MapId.Nullspace)
            return false;

        _mapManager.SetMapPaused(id, true);
        PausedMap = _mapManager.GetMapEntityId(id);

        if (PausedMap == null)
            return false;

        return true;
    }

    public bool IsInPausedMap(Entity<TransformComponent?> entity)
    {
        var (_, comp) = entity;
        comp ??= Transform(entity);

        return comp.MapUid != null && comp.MapUid == PausedMap;
    }

    public void BeforeEnter(EntityUid entity, EntityUid proxy)
    {
        if (!EnsurePausedMap())
            return;

        RecursiveAdd(entity, proxy);
    }

    public void AfterExit(EntityUid entity, EntityUid proxy)
    {
        RecursiveRemove(entity, proxy);
    }

    // Bottom to top
    private void RecursiveAdd(EntityUid entity, EntityUid proxy)
    {
        if (!TryComp<TransformComponent>(entity, out var entityXform))
            return;

        var enumerator = entityXform.ChildEnumerator;

        while (enumerator.MoveNext(out var child))
            RecursiveAdd(child, proxy);

        var ev = new BeforeEnterPausedMapEvent(entity, proxy);
        RaiseLocalEvent(entity, ref ev, true);

        AddComp(entity, new SharedPausedMapStorageComponent {
            Proxy = proxy
        });
    }

    // Bottom to top
    private void RecursiveRemove(EntityUid entity, EntityUid proxy)
    {
        if (!TryComp<TransformComponent>(entity, out var entityXform))
            return;

        var enumerator = entityXform.ChildEnumerator;

        while (enumerator.MoveNext(out var child))
            RecursiveAdd(child, proxy);

        var ev = new AfterExitPausedMapEvent(entity, proxy);
        RaiseLocalEvent(entity, ref ev, true);

        RemComp<SharedPausedMapStorageComponent>(entity);
    }
}

/// <summary>
/// Raised by <see cref="SharedPausedMapStorageSystem"><cref/> on an entity and all of it's children before they are sent to a paused storage map.
/// </summary>
/// <param name="Entity">An entity about to be sent. This may be a child entity of the main one.</param>
/// <param name="Proxy">The entity responsible for representing the parent entity, or the entity responsible for the parent entity's safe return.</param>
[ByRefEvent]
public readonly struct BeforeEnterPausedMapEvent
{
    public readonly EntityUid Entity;
    public readonly EntityUid Proxy;

    public BeforeEnterPausedMapEvent(EntityUid entity, EntityUid proxy)
    {
        Entity = entity;
        Proxy = proxy;
    }
}

/// <summary>
/// Raised by <see cref="SharedPausedMapStorageSystem"><cref/> on an entity and all of it's children after being returned from a paused storage map.
/// </summary>
/// <param name="Entity">An entity about to be returned. This may be a child entity of the main one.</param>
/// <param name="Proxy">The entity responsible for representing the parent entity, or the entity responsible for the parent entity's safe return.</param>
[ByRefEvent]
public readonly struct AfterExitPausedMapEvent
{
    public readonly EntityUid Entity;
    public readonly EntityUid Proxy;

    public AfterExitPausedMapEvent(EntityUid entity, EntityUid proxy)
    {
        Entity = entity;
        Proxy = proxy;
    }
}
