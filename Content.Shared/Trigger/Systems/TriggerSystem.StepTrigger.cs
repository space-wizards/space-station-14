using Content.Shared.StepTrigger.Systems;
using Content.Shared.Trigger.Components;
using Content.Shared.Trigger.Components.StepTriggers;
using Content.Shared.Trigger.Components.Triggers;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;

namespace Content.Shared.Trigger.Systems;

/// <summary>
/// Replacing <see cref="StepTriggerSystem"/>.
/// </summary>
public sealed partial class TriggerSystem
{
    private void InitializeStepTrigger()
    {
        SubscribeLocalEvent<TriggerStepLogicComponent, StartCollideEvent>(OnStepStart);
        SubscribeLocalEvent<TriggerStepLogicComponent, EndCollideEvent>(OnStepEnd);
        SubscribeLocalEvent<TriggerStepLogicComponent, AfterAutoHandleStateEvent>(TriggerHandleState);

        SubscribeLocalEvent<TriggerOnStepStartComponent, TriggerStepTriggeredOnEvent>(TriggerOnStepTriggeredOn);
        SubscribeLocalEvent<TriggerOnStepEndComponent, TriggerStepTriggeredOffEvent>(TriggerOnStepTriggeredOff);
        SubscribeLocalEvent<TriggerOnStepTriggerComponent, StepTriggeredOffEvent>(OnStepTriggered);
    }

    private void OnStepStart(Entity<TriggerStepLogicComponent> ent, ref StartCollideEvent args)
    {
        var uid = ent.Owner;
        var component = ent.Comp;
        var otherUid = args.OtherEntity;

        if (!args.OtherFixture.Hard)
            return;

        if (!CanTrigger(uid, otherUid, component))
            return;

        var evStep = new TriggerStepTriggeredOnEvent(uid, otherUid);
        RaiseLocalEvent(uid, ref evStep);

        EnsureComp<TriggerOnStepTriggerActiveComponent>(uid);

        if (component.Colliding.Add(otherUid))
        {
            Dirty(uid, component);
        }
    }

    private void OnStepEnd(Entity<TriggerStepLogicComponent> ent, ref EndCollideEvent args)
    {
        var uid = ent.Owner;
        var component = ent.Comp;
        var otherUid = args.OtherEntity;

        if (!component.Colliding.Remove(otherUid))
            return;

        component.CurrentlySteppedOn.Remove(otherUid);
        Dirty(uid, component);

        var evStepOff = new TriggerStepTriggeredOffEvent(uid, otherUid);
        RaiseLocalEvent(uid, ref evStepOff);

        if (component.Colliding.Count == 0)
        {
            RemCompDeferred<TriggerOnStepTriggerActiveComponent>(uid);
        }
    }

    /// <summary>
    /// Prediction is hard...
    /// though never use EnsureComp, RemCompDeferred, RemComp in <see cref="AfterAutoHandleStateEvent"/>.
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="args"></param>
    private void TriggerHandleState(Entity<TriggerStepLogicComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        var uid = ent.Owner;
        var component = ent.Comp;

        if (TryComp<TriggerOnStepTriggerActiveComponent>(uid, out var triggerActive))
            triggerActive.IsActive = component.Colliding.Count > 0;
    }

    private void UpdateStepTrigger()
    {
        var query = GetEntityQuery<PhysicsComponent>();
        var enumerator = EntityQueryEnumerator<TriggerOnStepTriggerActiveComponent, TriggerStepLogicComponent, TransformComponent>();

        while (enumerator.MoveNext(out var uid, out var active, out var trigger, out var transform))
        {
            if (!active.IsActive)
            {
                RemCompDeferred(uid, active);
                continue;
            }

            if (!UpdateStep(uid, trigger, transform, query))
            {
                continue;
            }

            RemCompDeferred(uid, active);
        }
    }

    private bool UpdateStep(EntityUid uid, TriggerStepLogicComponent component, TransformComponent transform, EntityQuery<PhysicsComponent> query)
    {
        if (!component.Active ||
            component.Colliding.Count == 0)
        {
            return true;
        }

        if (TryComp<TriggerStepLogicWithWhitelistComponent>(uid, out var whitelistComp) && TryComp<MapGridComponent>(transform.GridUid, out var grid))
        {
            var position = _map.LocalToTile(grid.Owner, grid, Transform(uid).Coordinates);
            var anchored = _map.GetAnchoredEntitiesEnumerator(uid, grid, position);

            while (anchored.MoveNext(out var ent))
            {
                if (ent == uid)
                    continue;

                if (_whitelist.IsWhitelistFail(whitelistComp.Whitelist, ent.Value)
                    && _whitelist.IsWhitelistPass(whitelistComp.Blacklist, ent.Value))
                    return false;
            }
        }
        foreach (var otherUid in component.Colliding)
        {
            UpdateColliding(uid, component, transform, otherUid, query);
        }

        return false;
    }

    /// <summary>
    /// Check is still collides.
    /// </summary>
    /// <remarks>
    /// TODO: Rewrite this AABBs cursed checks into something appropriate and more readable and maintainable.
    /// </remarks>
    /// <param name="ownerXform">Owner entity's transform/xform.</param>
    /// <param name="otherUid">Other entity's transform/xform that's stepped on the StepTrigger entity.</param>
    /// <param name="query">PhysicsQuery.</param>
    private void UpdateColliding(EntityUid uid, TriggerStepLogicComponent component, TransformComponent ownerXform, EntityUid otherUid, EntityQuery<PhysicsComponent> query)
    {
        if (!query.TryGetComponent(otherUid, out var otherPhysics))
            return;

        var otherXform = Transform(otherUid);
        // TODO: This shouldn't be calculating based on world AABBs.
        var ourAabb = _entityLookup.GetAABBNoContainer(uid, ownerXform.LocalPosition, ownerXform.LocalRotation);
        var otherAabb = _entityLookup.GetAABBNoContainer(otherUid, otherXform.LocalPosition, otherXform.LocalRotation);

        // Not collides atm
        if (!ourAabb.Intersects(otherAabb))
        {
            if (component.CurrentlySteppedOn.Remove(otherUid))
            {
                // Well, only because of this TriggerStepTriggeredOffEvent triggers twice.
                // But AABBs checks are cursed.
                // Maybe brave soul rewrite this.
                var evStepOff = new TriggerStepTriggeredOffEvent(uid, otherUid);
                RaiseLocalEvent(uid, ref evStepOff);
                Dirty(uid, component);
            }
            return;
        }

        // max 'area of enclosure' between the two aabbs
        // this is hard to explain
        var intersect = Box2.Area(otherAabb.Intersect(ourAabb));
        var ratio = Math.Max(intersect / Box2.Area(otherAabb), intersect / Box2.Area(ourAabb));
        if (otherPhysics.LinearVelocity.Length() < component.RequiredTriggeredSpeed
            || component.CurrentlySteppedOn.Contains(otherUid)
            || ratio < component.IntersectRatio
            || !CanTrigger(uid, otherUid, component))
        {
            return;
        }

        // Well, only because of this TriggerStepTriggeredOnEvent triggers twice.
        // But AABBs checks are cursed.
        // Maybe brave soul rewrite this.
        var evStepOn = new TriggerStepTriggeredOnEvent(uid, otherUid);
        RaiseLocalEvent(uid, ref evStepOn);

        component.CurrentlySteppedOn.Add(otherUid);
        Dirty(uid, component);
    }

    private bool CanTrigger(EntityUid uid, EntityUid otherUid, TriggerStepLogicComponent component)
    {
        if (!component.Active || component.CurrentlySteppedOn.Contains(otherUid))
            return false;

        // Can't trigger if we don't ignore weightless entities
        // and the entity is flying or currently weightless
        // Makes sense simulation wise to have this be part of steptrigger directly IMO
        if (!component.IgnoreWeightless && TryComp<PhysicsComponent>(otherUid, out var physics) &&
            (physics.BodyStatus == BodyStatus.InAir || _gravity.IsWeightless(otherUid)))
            return false;

        var msg = new TriggerStepAttemptEvent { Source = uid, Tripper = otherUid };
        RaiseLocalEvent(uid, ref msg);
        return msg.Continue && !msg.Cancelled;
    }

    private void TriggerOnStepTriggeredOn(Entity<TriggerOnStepStartComponent> ent, ref TriggerStepTriggeredOnEvent args)
    {
        Trigger(ent, args.Tripper, ent.Comp.KeyOut);
    }

    private void TriggerOnStepTriggeredOff(Entity<TriggerOnStepEndComponent> ent, ref TriggerStepTriggeredOffEvent args)
    {
        Trigger(ent, args.Tripper, ent.Comp.KeyOut);
    }

    /// <inheritdoc cref="StepTrigger.Systems.StepTriggerSystem"/>
    [Obsolete]
    private void OnStepTriggered(Entity<TriggerOnStepTriggerComponent> ent, ref StepTriggeredOffEvent args)
    {
        Trigger(ent, args.Tripper, ent.Comp.KeyOut);
    }
}

/// <summary>
/// Raised when an entity checks that's pass checks.
/// </summary>
/// <remarks>
/// By default, <see cref="TriggerStepAttemptEvent.Continue"/> is false,
/// use <see cref="TriggerOnStepAlwaysAttemptComponent"/> at to make it always work.
/// </remarks>
[ByRefEvent]
public struct TriggerStepAttemptEvent
{
    /// <summary>
    /// The entity that got triggered by Tripper.
    /// </summary>
    public EntityUid Source;

    /// <summary>
    /// The entity that triggering Source by stepped on event.
    /// </summary>
    public EntityUid Tripper;

    /// <summary>
    /// By default, is false and never pass attempt as is.
    /// </summary>
    public bool Continue;

    /// <summary>
    /// Set by systems which wish to cancel the step trigger event, regardless of event ordering.
    /// </summary>
    public bool Cancelled;
}

/// <summary>
/// Raised when an entity start stands on a steptrigger initially OR when an entity starts intersects a steptrigger.
/// </summary>
/// <remarks>
/// Be cautious, the event triggers twice because of AABBs checks.
/// </remarks>
[ByRefEvent]
public readonly record struct TriggerStepTriggeredOnEvent(EntityUid Source, EntityUid Tripper);

/// <summary>
/// Raised when an entity leaves a steptrigger OR when an entity stops intersects a steptrigger.
/// </summary>
/// <remarks>
/// Be cautious, the event triggers twice because of AABBs checks.
/// Raised after <see cref="TriggerStepTriggeredOnEvent"/>.
/// </remarks>
[ByRefEvent]
public readonly record struct TriggerStepTriggeredOffEvent(EntityUid Source, EntityUid Tripper);
