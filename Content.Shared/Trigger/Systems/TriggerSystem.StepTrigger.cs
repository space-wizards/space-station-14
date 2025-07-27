using Content.Shared.StepTrigger.Systems;
using Content.Shared.Trigger.Components;
using Content.Shared.Trigger.Components.StepTriggers;
using Content.Shared.Trigger.Components.Triggers;
using Robust.Shared.GameStates;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;

namespace Content.Shared.Trigger.Systems;

public sealed partial class TriggerSystem
{
    /// <summary>
    ///     Initialize subscriptions for the new step trigger logic, replacing <see cref="StepTriggerSystem"/>.
    /// </summary>
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

        if (component.StepOn)
        {
            var evStepOff = new TriggerStepTriggeredOffEvent(uid, otherUid);
            RaiseLocalEvent(uid, ref evStepOff);
        }

        if (component.Colliding.Count == 0)
        {
            RemCompDeferred<TriggerOnStepTriggerActiveComponent>(uid);
        }
    }

    private void TriggerHandleState(Entity<TriggerStepLogicComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        var uid = ent.Owner;
        var component = ent.Comp;

        if (component.Colliding.Count > 0)
        {
            EnsureComp<TriggerOnStepTriggerActiveComponent>(uid);
        }
        else
        {
            RemCompDeferred<TriggerOnStepTriggerActiveComponent>(uid);
        }
    }

    private void UpdateStepTrigger()
    {
        var query = GetEntityQuery<PhysicsComponent>();
        var enumerator = EntityQueryEnumerator<TriggerOnStepTriggerActiveComponent, TriggerStepLogicComponent, TransformComponent>();

        while (enumerator.MoveNext(out var uid, out var active, out var trigger, out var transform))
        {
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

        if (component.Blacklist != null && TryComp<MapGridComponent>(transform.GridUid, out var grid))
        {
            var position = _map.LocalToTile(grid.Owner, grid, Transform(uid).Coordinates);
            var anchored = _map.GetAnchoredEntitiesEnumerator(uid, grid, position);

            while (anchored.MoveNext(out var ent))
            {
                if (ent == uid)
                    continue;

                if (_whitelist.IsBlacklistPass(component.Blacklist, ent.Value))
                    return false;
            }
        }
        foreach (var otherUid in component.Colliding)
        {
            UpdateColliding(uid, component, transform, otherUid, query);
        }

        return false;
    }

    private void UpdateColliding(EntityUid uid, TriggerStepLogicComponent component, TransformComponent ownerXform, EntityUid otherUid, EntityQuery<PhysicsComponent> query)
    {
        if (!query.TryGetComponent(otherUid, out var otherPhysics))
            return;

        var otherXform = Transform(otherUid);
        // TODO: This shouldn't be calculating based on world AABBs.
        var ourAabb = _entityLookup.GetAABBNoContainer(uid, ownerXform.LocalPosition, ownerXform.LocalRotation);
        var otherAabb = _entityLookup.GetAABBNoContainer(otherUid, otherXform.LocalPosition, otherXform.LocalRotation);

        if (!ourAabb.Intersects(otherAabb))
        {
            if (component.CurrentlySteppedOn.Remove(otherUid))
            {
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

        if (component.StepOn)
        {
            var evStep = new TriggerStepTriggeredOnEvent(uid, otherUid);
            RaiseLocalEvent(uid, ref evStep);
        }
        else
        {
            var evStep = new TriggerStepTriggeredOffEvent(uid, otherUid);
            RaiseLocalEvent(uid, ref evStep);
        }

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
            (physics.BodyStatus == BodyStatus.InAir || _gravity.IsWeightless(otherUid, physics)))
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

[ByRefEvent]
public struct TriggerStepAttemptEvent
{
    public EntityUid Source;
    public EntityUid Tripper;
    public bool Continue;
    /// <summary>
    ///     Set by systems which wish to cancel the step trigger event, regardless of event ordering.
    /// </summary>
    public bool Cancelled;
}

/// <summary>
/// Raised when an entity stands on a steptrigger initially (assuming it has both on and off states).
/// </summary>
[ByRefEvent]
public readonly record struct TriggerStepTriggeredOnEvent(EntityUid Source, EntityUid Tripper);

/// <summary>
/// Raised when an entity leaves a steptrigger if it has on and off states OR when an entity intersects a steptrigger.
/// </summary>
[ByRefEvent]
public readonly record struct TriggerStepTriggeredOffEvent(EntityUid Source, EntityUid Tripper);

// [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
// public sealed partial class TriggerOnStepStartComponent : Component { }

// [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
// public sealed partial class TriggerOnStepEndComponent : Component { }

// [Obsolete("Uses legacy StepTriggerSystem. Use TriggerOnStepStartComponent and TriggerOnStepEndComponent instead.")]
// [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
// public sealed partial class TriggerOnStepTriggerComponent : BaseTriggerOnXComponent { }

// [RegisterComponent, NetworkedComponent]
// public sealed partial class TriggerOnStepTriggerActiveComponent : Component { }

// [RegisterComponent]
// public sealed partial class TriggerOnStepAliveAttemptComponent : Component { }

// [RegisterComponent]
// public sealed partial class TriggerOnStepTagAttemptComponent : Component { }

// [RegisterComponent]
// public sealed partial class TriggerOnStepWhitelistAttemptComponent : Component { }

// [RegisterComponent]
// public sealed partial class TriggerOnStepAlwaysAttemptComponent : Component { }
