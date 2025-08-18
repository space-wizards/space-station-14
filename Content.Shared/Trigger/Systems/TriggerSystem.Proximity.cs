using System.Numerics;
using Content.Shared.Examine;
using Content.Shared.Trigger.Components.Triggers;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;

namespace Content.Shared.Trigger.Systems;

public sealed partial class TriggerSystem
{
    private void InitializeProximity()
    {
        SubscribeLocalEvent<TriggerOnProximityComponent, StartCollideEvent>(OnProximityStartCollide);
        SubscribeLocalEvent<TriggerOnProximityComponent, EndCollideEvent>(OnProximityEndCollide);
        SubscribeLocalEvent<TriggerOnProximityComponent, MapInitEvent>(OnMapInit);
        // Shouldn't need re-anchoring.
        SubscribeLocalEvent<TriggerOnProximityComponent, AnchorStateChangedEvent>(OnProximityAnchor);
        SubscribeLocalEvent<TriggerOnProximityComponent, TriggerEvent>(OnProximityReceivingTrigger);
        SubscribeLocalEvent<TriggerOnProximityComponent, ExaminedEvent>(OnProximityExamined);
    }

    /// <summary>
    /// Gets the velocity of either an entity's grid, or itself if it is not on any grid.
    /// I.e., the velocity of the entity only relative to the map, unlike <see cref="PhysicsComponent.LinearVelocity"/>.
    /// </summary>
    private Vector2 GetAbsoluteVelocity(Entity<PhysicsComponent> entity)
    {
        if (!TryComp(entity, out TransformComponent? transformComponent))
            return Vector2.Zero;

        // If they have a grid, return the grid's velocity plus the velocity of the entity, relative to it.
        // If the trycomp somehow fails, it means the grid is static and therefore we can accurately assume that the entity's
        // absolute velocity is it's velocity relative to the grid.
        var entityLinearVelocity = entity.Comp.LinearVelocity;
        if (transformComponent.GridUid is { } gridUid &&
            _physicsQuery.TryComp(gridUid, out var gridPhysicsComponent))
        {
            return gridPhysicsComponent.LinearVelocity + entityLinearVelocity;
        }
        else
            return entityLinearVelocity;
    }

    /// <summary>
    /// Helper method for setting whether an entity is allowed to sleep. Will wake the entity if it's not allowed to sleep.
    /// </summary>
    /// <remarks>
    /// This is done because sleeping prox-triggers won't trigger for things that are on another grid, but still in range.
    private void UpdateProximityAwakeness(Entity<PhysicsComponent?> ent, bool value)
    {
        ref PhysicsComponent? physicsComponent = ref ent.Comp;
        if (!_physicsQuery.Resolve(ent, ref physicsComponent, false))
            return;

        _physics.SetSleepingAllowed(ent, physicsComponent, value);

        // If we're not allowed to sleep then might as well wake it.
        if (!value)
            _physics.WakeBody(ent, body: physicsComponent);
    }

    private void OnProximityExamined(Entity<TriggerOnProximityComponent> ent, ref ExaminedEvent args)
    {
        if (!ent.Comp.Examinable || !args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("proximity-trigger-examine", ("enabled", ent.Comp.Enabled)));
    }

    private void OnProximityReceivingTrigger(Entity<TriggerOnProximityComponent> ent, ref TriggerEvent args)
    {
        var proximityComponent = ent.Comp;
        var key = args.Key;

        if (key == null)
            return;

        // So, enable the comp if the key exists in `EnableKeysIn` even if it exists in `DisableKeysIn`.
        // If the key only exists in `DisableKeysIn`, then disable the comp.
        // If the key exists in neither, keep `Enabled` at it's original value.
        proximityComponent.Enabled =
            proximityComponent.EnablingKeysIn.Contains(key) ||
            (!proximityComponent.DisablingKeysIn.Contains(key) &&
             proximityComponent.Enabled);

        if (proximityComponent.TogglingKeysIn.Contains(key))
            proximityComponent.Enabled ^= true;

        // If you could manually enable/disable collision processing on fixtures then I'd do it here.
        // Surely it would save some performance, no?
        DirtyField(ent, proximityComponent, nameof(proximityComponent.Enabled));

        // If it's enabled, don't let it sleep, otherwise it won't work across grids.
        UpdateProximityAwakeness(ent.Owner, !proximityComponent.Enabled);

        SetProximityAppearance(ent);
    }

    private void OnProximityAnchor(Entity<TriggerOnProximityComponent> ent, ref AnchorStateChangedEvent args)
    {
        ent.Comp.Enabled = !ent.Comp.RequiresAnchored || args.Anchored;

        SetProximityAppearance(ent);

        if (!ent.Comp.Enabled)
        {
            ent.Comp.Colliding.Clear();
        }
        // Re-check for contacts as we cleared them.
        else if (_physicsQuery.TryGetComponent(ent, out var body))
        {
            // It's enabled so don't let it sleep.
            UpdateProximityAwakeness((ent.Owner, body), false);
            _physics.RegenerateContacts((ent.Owner, body));
        }

        Dirty(ent);
    }

    private void OnMapInit(Entity<TriggerOnProximityComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.Enabled = !ent.Comp.RequiresAnchored || Transform(ent).Anchored;

        SetProximityAppearance(ent);

        if (!_physicsQuery.TryGetComponent(ent, out var body))
            return;

        // If its enabled dont let it sleep.
        UpdateProximityAwakeness((ent.Owner, body), !ent.Comp.Enabled);
        _fixture.TryCreateFixture(
            ent.Owner,
            ent.Comp.Shape,
            TriggerOnProximityComponent.FixtureID,
            hard: false,
            body: body,
            collisionLayer: ent.Comp.Layer);

        Dirty(ent);
    }

    private void OnProximityStartCollide(EntityUid uid, TriggerOnProximityComponent component, ref StartCollideEvent args)
    {
        if (args.OurFixtureId != TriggerOnProximityComponent.FixtureID)
            return;

        component.Colliding[args.OtherEntity] = args.OtherBody;
    }

    private static void OnProximityEndCollide(EntityUid uid, TriggerOnProximityComponent component, ref EndCollideEvent args)
    {
        if (args.OurFixtureId != TriggerOnProximityComponent.FixtureID)
            return;

        component.Colliding.Remove(args.OtherEntity);
    }

    private void SetProximityAppearance(Entity<TriggerOnProximityComponent> ent)
    {
        _appearance.SetData(ent.Owner, ProximityTriggerVisualState.State, ent.Comp.Enabled ? ProximityTriggerVisuals.Inactive : ProximityTriggerVisuals.Off);
    }

    private void Activate(Entity<TriggerOnProximityComponent> ent, EntityUid user)
    {
        var curTime = _timing.CurTime;

        if (!ent.Comp.Repeating)
        {
            ent.Comp.Enabled = false;
            UpdateProximityAwakeness(ent.Owner, true);

            ent.Comp.Colliding.Clear();
        }
        else
        {
            ent.Comp.NextTrigger = curTime + ent.Comp.Cooldown;
        }

        // Queue a visual update for when the animation is complete.
        ent.Comp.NextVisualUpdate = curTime + ent.Comp.AnimationDuration;
        Dirty(ent);

        _appearance.SetData(ent.Owner, ProximityTriggerVisualState.State, ProximityTriggerVisuals.Active);

        Trigger(ent.Owner, user, ent.Comp.KeyOut);
    }

    private void UpdateProximity()
    {
        var curTime = _timing.CurTime;

        var query = EntityQueryEnumerator<TriggerOnProximityComponent>();
        while (query.MoveNext(out var uid, out var trigger))
        {
            if (curTime >= trigger.NextVisualUpdate)
            {
                // Update the visual state once the animation is done.
                trigger.NextVisualUpdate = TimeSpan.MaxValue;
                Dirty(uid, trigger);
                SetProximityAppearance((uid, trigger));
            }

            var colliding = trigger.Colliding;

            // Continue if we're disabled, on cooldown, or the list of colliding objects is empty.
            if (!trigger.Enabled ||
                curTime < trigger.NextTrigger ||
                colliding.Count == 0)
                continue;

            var ourVelocity = Vector2.Zero;
            if (_physicsQuery.TryGetComponent(uid, out var ourPhysicsComponent))
                ourVelocity = GetAbsoluteVelocity((uid, ourPhysicsComponent));

            var triggerSpeed = trigger.TriggerSpeed;

            // Check for anything colliding and moving fast enough, relative to us.
            foreach (var (collidingUid, collidingPhysics) in colliding)
            {
                if (TerminatingOrDeleted(collidingUid))
                    continue;

                if ((GetAbsoluteVelocity((collidingUid, collidingPhysics)) - ourVelocity).Length() < triggerSpeed)
                    continue;

                if (trigger.RequiresLineOfSight && !_examineSystem.InRangeUnOccluded(uid, collidingUid, range: trigger.Shape.Radius))
                    continue;

                // Trigger!
                Activate((uid, trigger), collidingUid);
                break;
            }
        }
    }
}
