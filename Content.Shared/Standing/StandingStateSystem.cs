using Content.Shared.Climbing.Events;
using Content.Shared.Hands.Components;
using Content.Shared.Inventory;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Physics;
using Content.Shared.Rotation;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Standing;

public sealed class StandingStateSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    // If StandingCollisionLayer value is ever changed to more than one layer, the logic needs to be edited.
    public const int StandingCollisionLayer = (int) CollisionGroup.MidImpassable;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StandingStateComponent, AttemptMobCollideEvent>(OnMobCollide);
        SubscribeLocalEvent<StandingStateComponent, AttemptMobTargetCollideEvent>(OnMobTargetCollide);
        SubscribeLocalEvent<StandingStateComponent, RefreshFrictionModifiersEvent>(OnRefreshFrictionModifiers);
        SubscribeLocalEvent<StandingStateComponent, TileFrictionEvent>(OnTileFriction);
        SubscribeLocalEvent<StandingStateComponent, EndClimbEvent>(OnEndClimb);
    }

    private void OnMobTargetCollide(Entity<StandingStateComponent> ent, ref AttemptMobTargetCollideEvent args)
    {
        if (!ent.Comp.Standing)
        {
            args.Cancelled = true;
        }
    }

    private void OnMobCollide(Entity<StandingStateComponent> ent, ref AttemptMobCollideEvent args)
    {
        if (!ent.Comp.Standing)
        {
            args.Cancelled = true;
        }
    }

    private void OnRefreshFrictionModifiers(Entity<StandingStateComponent> entity, ref RefreshFrictionModifiersEvent args)
    {
        if (entity.Comp.Standing)
            return;

        args.ModifyFriction(entity.Comp.DownFrictionMod);
        args.ModifyAcceleration(entity.Comp.DownFrictionMod);
    }

    private void OnTileFriction(Entity<StandingStateComponent> entity, ref TileFrictionEvent args)
    {
        if (!entity.Comp.Standing)
            args.Modifier *= entity.Comp.DownFrictionMod;
    }

    private void OnEndClimb(Entity<StandingStateComponent> entity, ref EndClimbEvent args)
    {
        if (entity.Comp.Standing)
            return;

        // Currently only Climbing also edits fixtures layers like this so this is fine for now.
        ChangeLayers(entity);
    }

    public bool IsMatchingState(Entity<StandingStateComponent?> entity, bool standing)
    {
        return standing != IsDown(entity);
    }

    public bool IsDown(Entity<StandingStateComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return false;

        return !entity.Comp.Standing;
    }

    public bool Down(EntityUid uid,
        bool playSound = true,
        bool dropHeldItems = true,
        bool force = false,
        StandingStateComponent? standingState = null,
        AppearanceComponent? appearance = null,
        HandsComponent? hands = null)
    {
        // TODO: This should actually log missing comps...
        if (!Resolve(uid, ref standingState, false))
            return false;

        // Optional component.
        Resolve(uid, ref appearance, ref hands, false);

        if (!standingState.Standing)
            return true;

        // This is just to avoid most callers doing this manually saving boilerplate
        // 99% of the time you'll want to drop items but in some scenarios (e.g. buckling) you don't want to.
        // We do this BEFORE downing because something like buckle may be blocking downing but we want to drop hand items anyway
        // and ultimately this is just to avoid boilerplate in Down callers + keep their behavior consistent.
        if (dropHeldItems && hands != null)
        {
            var ev = new DropHandItemsEvent();
            RaiseLocalEvent(uid, ref ev, false);
        }

        if (!force)
        {
            var msg = new DownAttemptEvent();
            RaiseLocalEvent(uid, msg, false);

            if (msg.Cancelled)
                return false;
        }

        standingState.Standing = false;
        Dirty(uid, standingState);
        RaiseLocalEvent(uid, new DownedEvent(), false);

        // Seemed like the best place to put it
        _appearance.SetData(uid, RotationVisuals.RotationState, RotationState.Horizontal, appearance);

        // Change collision masks to allow going under certain entities like flaps and tables
        ChangeLayers((uid, standingState));

        // check if component was just added or streamed to client
        // if true, no need to play sound - mob was down before player could seen that
        if (standingState.LifeStage <= ComponentLifeStage.Starting)
            return true;

        if (playSound)
        {
            _audio.PlayPredicted(standingState.DownSound, uid, uid);
        }

        return true;
    }

    public bool Stand(EntityUid uid,
        StandingStateComponent? standingState = null,
        AppearanceComponent? appearance = null,
        bool force = false)
    {
        // TODO: This should actually log missing comps...
        if (!Resolve(uid, ref standingState, false))
            return false;

        // Optional component.
        Resolve(uid, ref appearance, false);

        if (standingState.Standing)
            return true;

        if (!force)
        {
            var msg = new StandAttemptEvent();
            RaiseLocalEvent(uid, msg, false);

            if (msg.Cancelled)
                return false;
        }

        standingState.Standing = true;
        Dirty(uid, standingState);
        RaiseLocalEvent(uid, new StoodEvent(), false);

        _appearance.SetData(uid, RotationVisuals.RotationState, RotationState.Vertical, appearance);

        RevertLayers((uid, standingState));

        return true;
    }

    // TODO: This should be moved to a PhysicsModifierSystem which raises events so multiple systems can modify fixtures at once
    private void ChangeLayers(Entity<StandingStateComponent, FixturesComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp2, false))
            return;

        foreach (var (key, fixture) in entity.Comp2.Fixtures)
        {
            if ((fixture.CollisionMask & StandingCollisionLayer) == 0 || !fixture.Hard)
                continue;

            entity.Comp1.ChangedFixtures.Add(key);
            _physics.SetCollisionMask(entity, key, fixture, fixture.CollisionMask & ~StandingCollisionLayer, manager: entity.Comp2);
        }
    }

    // TODO: This should be moved to a PhysicsModifierSystem which raises events so multiple systems can modify fixtures at once
    private void RevertLayers(Entity<StandingStateComponent, FixturesComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp2, false))
        {
            entity.Comp1.ChangedFixtures.Clear();
            return;
        }

        foreach (var key in entity.Comp1.ChangedFixtures)
        {
            if (entity.Comp2.Fixtures.TryGetValue(key, out var fixture) && fixture.Hard)
                _physics.SetCollisionMask(entity, key, fixture, fixture.CollisionMask | StandingCollisionLayer, entity.Comp2);
        }

        entity.Comp1.ChangedFixtures.Clear();
    }
}

[ByRefEvent]
public record struct DropHandItemsEvent();

/// <summary>
/// Subscribe if you can potentially block a down attempt.
/// </summary>
public sealed class DownAttemptEvent : CancellableEntityEventArgs;

/// <summary>
/// Subscribe if you can potentially block a stand attempt.
/// </summary>
public sealed class StandAttemptEvent : CancellableEntityEventArgs;

/// <summary>
/// Raised when an entity becomes standing
/// </summary>
public sealed class StoodEvent : EntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots { get; } = SlotFlags.FEET;
};

/// <summary>
/// Raised when an entity is not standing
/// </summary>
public sealed class DownedEvent : EntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots { get; } = SlotFlags.FEET;
}

/// <summary>
/// Raised on an inhand entity being held by an entity who is dropping items as part of an attempted state change to down.
/// If cancelled the inhand entity will not be dropped.
/// </summary>
[ByRefEvent]
public record struct FellDownThrowAttemptEvent(EntityUid Thrower, bool Cancelled = false);
