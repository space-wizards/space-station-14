using Content.Server.Explosion.Components;
using Content.Shared.Physics;
using Content.Shared.Trigger;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Utility;

namespace Content.Server.Explosion.EntitySystems;

public sealed partial class TriggerSystem
{
    /// <summary>
    /// Anything that has stuff touching it (to check speed) or is on cooldown.
    /// </summary>
    private HashSet<TriggerOnProximityComponent> _activeProximities = new();

    private void InitializeProximity()
    {
        SubscribeLocalEvent<TriggerOnProximityComponent, StartCollideEvent>(OnProximityStartCollide);
        SubscribeLocalEvent<TriggerOnProximityComponent, EndCollideEvent>(OnProximityEndCollide);
        SubscribeLocalEvent<TriggerOnProximityComponent, MapInitEvent>(OnMapInit);
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
            _activeProximities.Remove(component);
            component.Colliding.Clear();
        }
        // Re-check for contacts as we cleared them.
        else if (TryComp<PhysicsComponent>(uid, out var body))
        {
            _broadphase.RegenerateContacts(body);
        }
    }

    private void OnProximityShutdown(EntityUid uid, TriggerOnProximityComponent component, ComponentShutdown args)
    {
        _activeProximities.Remove(component);
        component.Colliding.Clear();
    }

    private void OnMapInit(EntityUid uid, TriggerOnProximityComponent component, MapInitEvent args)
    {
        component.Enabled = !component.RequiresAnchored ||
                            EntityManager.GetComponent<TransformComponent>(uid).Anchored;

        SetProximityAppearance(uid, component);

        if (!TryComp<PhysicsComponent>(uid, out var body)) return;

        _fixtures.TryCreateFixture(body, new Fixture(body, component.Shape)
        {
            // TODO: Should probably have these settable via datafield but I'm lazy and it's a pain
            CollisionLayer = (int) (CollisionGroup.MidImpassable | CollisionGroup.LowImpassable | CollisionGroup.HighImpassable), Hard = false, ID = TriggerOnProximityComponent.FixtureID
        });
    }

    private void OnProximityStartCollide(EntityUid uid, TriggerOnProximityComponent component, ref StartCollideEvent args)
    {
        if (args.OurFixture.ID != TriggerOnProximityComponent.FixtureID) return;

        _activeProximities.Add(component);
        component.Colliding.Add(args.OtherFixture.Body);
    }

    private static void OnProximityEndCollide(EntityUid uid, TriggerOnProximityComponent component, ref EndCollideEvent args)
    {
        if (args.OurFixture.ID != TriggerOnProximityComponent.FixtureID) return;

        component.Colliding.Remove(args.OtherFixture.Body);
    }

    private void SetProximityAppearance(EntityUid uid, TriggerOnProximityComponent component)
    {
        if (EntityManager.TryGetComponent(uid, out AppearanceComponent? appearanceComponent))
        {
            appearanceComponent.SetData(ProximityTriggerVisualState.State, component.Enabled ? ProximityTriggerVisuals.Inactive : ProximityTriggerVisuals.Off);
        }
    }

    private void Activate(TriggerOnProximityComponent component)
    {
        DebugTools.Assert(component.Enabled);

        if (!component.Repeating)
        {
            component.Enabled = false;
            _activeProximities.Remove(component);
            component.Colliding.Clear();
        }
        else
        {
            component.Accumulator += component.Cooldown;
        }

        if (EntityManager.TryGetComponent(component.Owner, out AppearanceComponent? appearanceComponent))
        {
            appearanceComponent.SetData(ProximityTriggerVisualState.State, ProximityTriggerVisuals.Active);
        }

        Trigger(component.Owner);
    }

    private void UpdateProximity(float frameTime)
    {
        var toRemove = new RemQueue<TriggerOnProximityComponent>();

        foreach (var comp in _activeProximities)
        {
            MetaDataComponent? metadata = null;

            if (Deleted(comp.Owner, metadata))
            {
                toRemove.Add(comp);
                continue;
            }

            SetProximityAppearance(comp.Owner, comp);

            if (Paused(comp.Owner, metadata)) continue;

            comp.Accumulator -= frameTime;

            if (comp.Accumulator > 0f) continue;

            // Only remove it from accumulation when nothing colliding anymore.
            if (!comp.Enabled || comp.Colliding.Count == 0)
            {
                comp.Accumulator = 0f;
                toRemove.Add(comp);
                continue;
            }

            // Alright now that we have no cd check everything in range.

            foreach (var colliding in comp.Colliding)
            {
                if (Deleted(colliding.Owner)) continue;

                if (colliding.LinearVelocity.Length < comp.TriggerSpeed) continue;

                // Trigger!
                Activate(comp);
                break;
            }
        }

        foreach (var prox in toRemove)
        {
            _activeProximities.Remove(prox);
        }
    }
}
