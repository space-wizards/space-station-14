using Content.Shared.Gravity;
using Content.Shared.Maps;
using Content.Shared.StandTrigger.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;

namespace Content.Shared.StandTrigger.Systems;

/// <summary>
/// Used to trigger on entities that stand on top of the entity with <see cref="StandTriggerComponent"/>.
/// </summary>
public sealed class StandTriggerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly SharedGravitySystem _gravitySystem = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly EntityLookupSystem _lookupSystem = default!;
    [Dependency] private readonly TurfSystem _turfSystem = default!;

    // List of entities that are currently colliding with some other entity.
    private readonly HashSet<EntityUid> _colliding = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StandTriggerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<StandTriggerComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<StandTriggerComponent, EndCollideEvent>(OnEndCollide);
    }

    private void OnMapInit(Entity<StandTriggerComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = _timing.CurTime + ent.Comp.UpdateInterval;
        Dirty(ent);
    }

    private void OnStartCollide(Entity<StandTriggerComponent> ent, ref StartCollideEvent args)
    {
        if (!args.OtherFixture.Hard)
            return;

        if (!ent.Comp.Colliding.Add(args.OtherEntity))
            return;

        _colliding.Add(ent);

        Dirty(ent);
    }

    private void OnEndCollide(Entity<StandTriggerComponent> ent, ref EndCollideEvent args)
    {
        if (!ent.Comp.Colliding.Remove(args.OtherEntity))
            return;

        if (ent.Comp.Colliding.Count == 0)
            _colliding.Remove(ent);

        Dirty(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var uid in _colliding)
        {
            if (!TryComp<StandTriggerComponent>(uid, out var trigger))
            {
                // We don't need to keep track of entities that are no longer have trigger component.
                _colliding.Remove(uid);

                continue;
            }

            if (trigger.NextUpdate > _timing.CurTime)
                continue;

            trigger.NextUpdate += trigger.UpdateInterval;
            Dirty(uid, trigger);

            // Check if the tile is blocked with something from the blacklist.
            if (IsBlocked((uid, trigger)))
                continue;

            foreach (var otherUid in trigger.Colliding)
            {
                // Check if they can trigger this entity.
                if (!CanTrigger((uid, trigger), otherUid))
                    continue;

                var ev = new StandTriggerEvent(uid, otherUid);
                RaiseLocalEvent(uid, ref ev);
            }
        }
    }

    /// <summary>
    /// Used to determine whether or not something is blocking an entity.
    /// </summary>
    private bool IsBlocked(Entity<StandTriggerComponent> ent, TransformComponent? transform = null)
    {
        if (!Resolve(ent, ref transform) || ent.Comp.Blacklist == null ||
            !TryComp<MapGridComponent>(transform.GridUid, out var grid))
            return false;

        var positon = _mapSystem.LocalToTile(transform.GridUid.Value, grid, transform.Coordinates);
        var anchored = _mapSystem.GetAnchoredEntitiesEnumerator(ent, grid, positon);

        while (anchored.MoveNext(out var uid))
        {
            if (ent == uid)
                continue;

            if (_whitelistSystem.IsBlacklistPass(ent.Comp.Blacklist, uid.Value))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Used to determine whether or not an entity can be triggered by another one.
    /// </summary>
    private bool CanTrigger(Entity<StandTriggerComponent> ent, EntityUid otherUid)
    {
        if (!ent.Comp.IgnoreWeightless && TryComp<PhysicsComponent>(otherUid, out var physics) &&
            (physics.BodyStatus == BodyStatus.InAir || _gravitySystem.IsWeightless(otherUid, physics)))
            return false;

        var ev = new StandTriggerAttemptEvent(ent, otherUid);
        RaiseLocalEvent(ent, ev);

        return !ev.Cancelled;
    }
}

/// <summary>
/// Raised to determine whether or not an entity should trigger another entity that has a <see cref="StandTriggerComponent"/>.
/// </summary>
public sealed class StandTriggerAttemptEvent(EntityUid source, EntityUid tripper) : CancellableEntityEventArgs
{
    /// <summary>
    /// Entity that has a <see cref="StandTriggerComponent"/>.
    /// </summary>
    public EntityUid Source = source;

    /// <summary>
    /// Entity that stands on an entity with a <see cref="StandTriggerComponent"/>.
    /// </summary>
    public EntityUid Tripper = tripper;
}

/// <summary>
/// Raised on the entity with <see cref="StandTriggerComponent"/> when some entity triggers it.
/// </summary>
[ByRefEvent]
public readonly record struct StandTriggerEvent(EntityUid Source, EntityUid Tripper);
