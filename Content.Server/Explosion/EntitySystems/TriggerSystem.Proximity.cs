using Content.Server.Explosion.Components;
using Content.Shared.Trigger;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Utility;
using Robust.Shared.Physics;
using System.Linq;
using Content.Shared.Physics;
using Robust.Shared.Timing;

namespace Content.Server.Explosion.EntitySystems;

public sealed partial class TriggerSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    private void InitializeProximity()
    {
        SubscribeLocalEvent<TriggerOnProximityComponent, StartCollideEvent>(OnProximityStartCollide);
        SubscribeLocalEvent<TriggerOnProximityComponent, EndCollideEvent>(OnProximityEndCollide);
        SubscribeLocalEvent<TriggerOnProximityComponent, MapInitEvent>(OnMapInit);
        //SubscribeLocalEvent<TriggerOnProximityComponent, EntityUnpausedEvent>(OnUnpaused);
        SubscribeLocalEvent<TriggerOnProximityComponent, ComponentShutdown>(OnProximityShutdown);
        // Shouldn't need re-anchoring.
        SubscribeLocalEvent<TriggerOnProximityComponent, AnchorStateChangedEvent>(OnProximityAnchor);
    }

    private void OnProximityAnchor(EntityUid uid, TriggerOnProximityComponent component, ref AnchorStateChangedEvent args)
    {
        component.Enabled = !component.RequiresAnchored ||
                            args.Anchored;

        SetProximityAppearance(uid, component);

        if (!component.Enabled)
        {
            component.Colliding.Clear();
        }
        // Re-check for contacts as we cleared them.
        else if (TryComp<PhysicsComponent>(uid, out var body))
        {
            _broadphase.RegenerateContacts(uid, body);
        }
    }

    private void OnProximityShutdown(EntityUid uid, TriggerOnProximityComponent component, ComponentShutdown args)
    {
        component.Colliding.Clear();
    }

    private void OnMapInit(EntityUid uid, TriggerOnProximityComponent component, MapInitEvent args)
    {
        component.Enabled = !component.RequiresAnchored ||
                            Transform(uid).Anchored;

        SetProximityAppearance(uid, component);

        if (!TryComp<PhysicsComponent>(uid, out var body))
            return;

        _fixtures.TryCreateFixture(
            uid,
            component.Shape,
            TriggerOnProximityComponent.FixtureID,
            hard: false,
            collisionLayer: component.Layer);
    }

    private bool ShouldCollideThroughWall(EntityUid uid, TriggerOnProximityComponent component, ref StartCollideEvent args)
    {
        if (component.TriggerBehindWall)
            return true;


        if (TryComp<TransformComponent>(uid, out var entTransform) &&
                TryComp<TransformComponent>(args.OtherBody.Owner, out var otherTranform))
        {
            var physSystem = EntityManager.System<SharedPhysicsSystem>();

            var startPos = otherTranform.MapPosition.Position;
            var endPos = entTransform.MapPosition.Position;

            float distance = (startPos - endPos).Length();
            var direction = (endPos - startPos).Normalized();

            // If entity that caused activation is so close that direction becomes non-1, we assume that it can collide
            if (!MathHelper.CloseToPercent(direction.LengthSquared(), 1))
                return true;

            var rayRes = physSystem.IntersectRay(otherTranform.MapID, new CollisionRay(startPos, direction, (int)CollisionGroup.Impassable), distance/2).ToList();

            if (!rayRes.Any())
                return true;
        }
        return false;
    }

    private void OnProximityStartCollide(EntityUid uid, TriggerOnProximityComponent component, ref StartCollideEvent args)
    {
        if (args.OurFixture.ID != TriggerOnProximityComponent.FixtureID)
            return;

        if (!ShouldCollideThroughWall(uid, component, ref args))
            return;

        component.Colliding[args.OtherEntity] = args.OtherBody;
    }

    private static void OnProximityEndCollide(EntityUid uid, TriggerOnProximityComponent component, ref EndCollideEvent args)
    {
        if (args.OurFixture.ID != TriggerOnProximityComponent.FixtureID)
            return;

        component.Colliding.Remove(args.OtherEntity);
    }

    private void SetProximityAppearance(EntityUid uid, TriggerOnProximityComponent component)
    {
        if (EntityManager.TryGetComponent(uid, out AppearanceComponent? appearance))
        {
            _appearance.SetData(uid, ProximityTriggerVisualState.State, component.Enabled ? ProximityTriggerVisuals.Inactive : ProximityTriggerVisuals.Off, appearance);
        }
    }

    private void Activate(EntityUid uid, EntityUid user, TriggerOnProximityComponent component)
    {
        DebugTools.Assert(component.Enabled);

        var curTime = _timing.CurTime;

        if (!component.Repeating)
        {
            component.Enabled = false;
            component.Colliding.Clear();
        }
        else
        {
            component.NextTrigger = curTime + component.Cooldown;
        }

        // Queue a visual update for when the animation is complete.
        component.NextVisualUpdate = curTime + component.AnimationDuration;

        if (EntityManager.TryGetComponent(uid, out AppearanceComponent? appearance))
        {
            _appearance.SetData(uid, ProximityTriggerVisualState.State, ProximityTriggerVisuals.Active, appearance);
        }

        Trigger(uid, user);
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
                SetProximityAppearance(uid, trigger);
            }

            if (!trigger.Enabled)
                continue;

            if (curTime < trigger.NextTrigger)
                // The trigger's on cooldown.
                continue;

            // Check for anything colliding and moving fast enough.
            foreach (var (collidingUid, colliding) in trigger.Colliding)
            {
                if (Deleted(collidingUid))
                    continue;

                if (colliding.LinearVelocity.Length() < trigger.TriggerSpeed)
                    continue;

                // Trigger!
                Activate(uid, collidingUid, trigger);
                break;
            }
        }
    }
}
