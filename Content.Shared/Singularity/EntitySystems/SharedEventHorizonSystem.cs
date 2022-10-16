using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

using Content.Shared.Ghost;
using Content.Shared.Singularity.Components;

namespace Content.Shared.Singularity.EntitySystems;

public abstract class SharedEventHorizonSystem : EntitySystem
{
    [Dependency] private readonly FixtureSystem _fixtures = default!;

    public const string DefaultHorizonFixtureId = "EventHorizon";


#region Getters/Setters

    /// <summary>
    ///     Sets the radius of an event horizon to a new value.
    /// </summary>
    public void SetRadius(EntityUid uid, float value, bool updateFixture = true, SharedEventHorizonComponent? eventHorizon = null)
    {
        if(!Resolve(uid, ref eventHorizon))
            return;

        var oldValue = eventHorizon._radius;
        if (value == oldValue)
            return;

        eventHorizon._radius = value;
        eventHorizon.Dirty();
        if (updateFixture)
            UpdateEventHorizonFixture(uid, eventHorizon, null);
    }

    /// <summary>
    ///     Sets the radius of an event horizon to a new value.
    /// </summary>
    public void SetCanBreachContainment(EntityUid uid, bool value, bool updateFixture = true, SharedEventHorizonComponent? eventHorizon = null)
    {
        if(!Resolve(uid, ref eventHorizon))
            return;

        var oldValue = eventHorizon._canBreachContainment;
        if (value == oldValue)
            return;

        eventHorizon._canBreachContainment = value;
        eventHorizon.Dirty();
        if (updateFixture)
            UpdateEventHorizonFixture(uid, eventHorizon, null);
    }

    /// <summary>
    ///     Sets the radius of an event horizon to a new value.
    /// </summary>
    public void UpdateEventHorizonFixture(EntityUid uid, SharedEventHorizonComponent? eventHorizon, PhysicsComponent? fixtures)
    {
        if(!Resolve(uid, ref eventHorizon))
            return;

        var fixtureId = eventHorizon.HorizonFixtureId;
        if (fixtureId == null || !Resolve(uid, ref fixtures))
            return;

        var fixture = _fixtures.GetFixtureOrNull(fixtures, fixtureId);
        if (fixture == null)
            return;

        var shape = (PhysShapeCircle)fixture.Shape;
        shape.Radius = eventHorizon.Radius;
        fixture.Hard = !eventHorizon.CanBreachContainment;
        fixtures.Dirty();
    }

#endregion Getters/Setters


#region EventHandlers

    /// <summary>
    ///
    /// </summary>
    private void OnPreventCollide(EntityUid uid, SharedEventHorizonComponent comp, ref PreventCollideEvent args)
    {
        if(!args.Cancelled)
            PreventCollide(uid, comp, ref args);
    }

    protected virtual bool PreventCollide(EntityUid uid, SharedEventHorizonComponent comp, ref PreventCollideEvent args)
    {
        var otherUid = args.BodyB.Owner;

        // For prediction reasons always want the client to ignore these.
        if (EntityManager.HasComponent<IMapGridComponent>(otherUid) ||
            EntityManager.HasComponent<SharedGhostComponent>(otherUid))
        {
            args.Cancelled = true;
            return true;
        }

        // If we're above 4 then breach containment
        // otherwise, check if it's containment and just keep the collision
        if (EntityManager.HasComponent<SharedContainmentFieldComponent>(otherUid) ||
            EntityManager.HasComponent<SharedContainmentFieldGeneratorComponent>(otherUid))
        {
            if (comp.CanBreachContainment)
                args.Cancelled = true;

            return true;
        }

        return false;
    }

#endregion EventHandlers
}
