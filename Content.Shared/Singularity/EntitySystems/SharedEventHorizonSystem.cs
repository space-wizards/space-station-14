using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

using Content.Shared.Ghost;
using Content.Shared.Singularity.Components;

namespace Content.Shared.Singularity.EntitySystems;

/// <summary>
/// The entity system primarily responsible for managing <see cref="EventHorizonComponent"/>s.
/// </summary>
public abstract class SharedEventHorizonSystem : EntitySystem
{
#region Dependencies
    [Dependency] private readonly FixtureSystem _fixtures = default!;
    [Dependency] protected readonly IViewVariablesManager Vvm = default!;
#endregion Dependencies
    public override void Initialize()
    {
        base.Initialize();

        // Allows for predicted collisions with singularities.
        SubscribeLocalEvent<EventHorizonComponent, ComponentStartup>(OnEventHorizonStartup);
        SubscribeLocalEvent<EventHorizonComponent, PreventCollideEvent>(OnPreventCollide);

        var vvHandle = Vvm.GetTypeHandler<EventHorizonComponent>();
        vvHandle.AddPath(nameof(EventHorizonComponent.Radius), (_, comp) => comp.Radius, (uid, value, comp) => SetRadius(uid, value, eventHorizon: comp));
        vvHandle.AddPath(nameof(EventHorizonComponent.CanBreachContainment), (_, comp) => comp.CanBreachContainment, (uid, value, comp) => SetCanBreachContainment(uid, value, eventHorizon: comp));
        vvHandle.AddPath(nameof(EventHorizonComponent.HorizonFixtureId), (_, comp) => comp.HorizonFixtureId, (uid, value, comp) => SetHorizonFixtureId(uid, value, eventHorizon: comp));
    }

    public override void Shutdown()
    {
        var vvHandle = Vvm.GetTypeHandler<EventHorizonComponent>();
        vvHandle.RemovePath(nameof(EventHorizonComponent.Radius));
        vvHandle.RemovePath(nameof(EventHorizonComponent.CanBreachContainment));
        vvHandle.RemovePath(nameof(EventHorizonComponent.HorizonFixtureId));

        base.Shutdown();
    }

#region Getters/Setters

    /// <summary>
    /// Setter for <see cref="EventHorizonComponent.Radius"/>
    /// May also update the fixture associated with the event horizon.
    /// </summary>
    /// <param name="uid">The uid of the event horizon to change the radius of.</param>
    /// <param name="value">The new radius of the event horizon.</param>
    /// <param name="updateFixture">Whether to update the associated fixture upon changing the radius of the event horizon.</param>
    /// <param name="eventHorizon">The state of the event horizon to change the radius of.</param>
    public void SetRadius(EntityUid uid, float value, bool updateFixture = true, EventHorizonComponent? eventHorizon = null)
    {
        if(!Resolve(uid, ref eventHorizon))
            return;

        var oldValue = eventHorizon.Radius;
        if (value == oldValue)
            return;

        eventHorizon.Radius = value;
        EntityManager.Dirty(eventHorizon);
        if (updateFixture)
            UpdateEventHorizonFixture(uid, eventHorizon: eventHorizon);
    }

    /// <summary>
    /// Setter for <see cref="EventHorizonComponent.CanBreachContainment"/>
    /// May also update the fixture associated with the event horizon.
    /// </summary>
    /// <param name="uid">The uid of the event horizon to make (in)capable of breaching containment.</param>
    /// <param name="value">Whether the event horizon should be able to breach containment.</param>
    /// <param name="updateFixture">Whether to update the associated fixture upon changing whether the event horizon can breach containment.</param>
    /// <param name="eventHorizon">The state of the event horizon to make (in)capable of breaching containment.</param>
    public void SetCanBreachContainment(EntityUid uid, bool value, bool updateFixture = true, EventHorizonComponent? eventHorizon = null)
    {
        if(!Resolve(uid, ref eventHorizon))
            return;

        var oldValue = eventHorizon.CanBreachContainment;
        if (value == oldValue)
            return;

        eventHorizon.CanBreachContainment = value;
        EntityManager.Dirty(eventHorizon);
        if (updateFixture)
            UpdateEventHorizonFixture(uid, eventHorizon: eventHorizon);
    }

    /// <summary>
    /// Setter for <see cref="EventHorizonComponent.HorizonFixtureId"/>
    /// May also update the fixture associated with the event horizon.
    /// </summary>
    /// <param name="uid">The uid of the event horizon with the fixture ID to change.</param>
    /// <param name="value">The new fixture ID to associate the event horizon with.</param>
    /// <param name="updateFixture">Whether to update the associated fixture upon changing whether the event horizon can breach containment.</param>
    /// <param name="eventHorizon">The state of the event horizon with the fixture ID to change.</param>
    public void SetHorizonFixtureId(EntityUid uid, string? value, bool updateFixture = true, EventHorizonComponent? eventHorizon = null)
    {
        if(!Resolve(uid, ref eventHorizon))
            return;

        var oldValue = eventHorizon.HorizonFixtureId;
        if (value == oldValue)
            return;

        eventHorizon.HorizonFixtureId = value;
        EntityManager.Dirty(eventHorizon);
        if (updateFixture)
            UpdateEventHorizonFixture(uid, eventHorizon: eventHorizon);
    }

    /// <summary>
    /// Updates the state of the fixture associated with the event horizon.
    /// </summary>
    /// <param name="eventHorizon">The uid of the event horizon associated with the fixture to update.</param>
    /// <param name="fixtures">The physics component containing the fixture to update.</param>
    /// <param name="eventHorizon">The state of the event horizon associated with the fixture to update.</param>
    public void UpdateEventHorizonFixture(EntityUid uid, PhysicsComponent? fixtures = null, EventHorizonComponent? eventHorizon = null)
    {
        if(!Resolve(uid, ref eventHorizon))
            return;

        var fixtureId = eventHorizon.HorizonFixtureId;
        if (fixtureId == null || !Resolve(eventHorizon.Owner, ref fixtures, logMissing: false))
            return;

        var fixture = _fixtures.GetFixtureOrNull(fixtures, fixtureId);
        if (fixture == null)
            return;

        var shape = (PhysShapeCircle)fixture.Shape;
        shape.Radius = eventHorizon.Radius;
        fixture.Hard = !eventHorizon.CanBreachContainment;
        EntityManager.Dirty(fixtures);
    }

#endregion Getters/Setters


#region EventHandlers

    /// <summary>
    /// Syncs the state of the fixture associated with the event horizon upon startup.
    /// </summary>
    /// <param name="uid">The entity that has just gained an event horizon component.</param>
    /// <param name="comp">The event horizon component that is starting up.</param>
    /// <param name="args">The event arguments.</param>
    private void OnEventHorizonStartup(EntityUid uid, EventHorizonComponent comp, ComponentStartup args)
    {
        UpdateEventHorizonFixture(uid, eventHorizon: comp);
    }

    /// <summary>
    /// Prevents the event horizon from colliding with anything it cannot consume.
    /// Most notably map grids and ghosts.
    /// Also makes event horizons phase through containment if it can breach.
    /// </summary>
    /// <param name="uid">The entity that is trying to collide with another entity.</param>
    /// <param name="comp">The event horizon of the former.</param>
    /// <param name="args">The event arguments.</param>
    private void OnPreventCollide(EntityUid uid, EventHorizonComponent comp, ref PreventCollideEvent args)
    {
        if(!args.Cancelled)
            PreventCollide(uid, comp, ref args);
    }

    /// <summary>
    /// The actual, functional part of SharedEventHorizonSystem.OnPreventCollide.
    /// The return value allows for overrides to early return if the base successfully handles collision prevention.
    /// </summary>
    /// <param name="uid">The entity that is trying to collide with another entity.</param>
    /// <param name="comp">The event horizon of the former.</param>
    /// <param name="args">The event arguments.</param>
    /// <returns>A bool indicating whether the collision prevention has been handled.</return>
    protected virtual bool PreventCollide(EntityUid uid, EventHorizonComponent comp, ref PreventCollideEvent args)
    {
        var otherUid = args.BodyB.Owner;

        // For prediction reasons always want the client to ignore these.
        if (EntityManager.HasComponent<MapGridComponent>(otherUid) ||
            EntityManager.HasComponent<SharedGhostComponent>(otherUid))
        {
            args.Cancelled = true;
            return true;
        }

        // If we can, breach containment
        // otherwise, check if it's containment and just keep the collision
        if (EntityManager.HasComponent<ContainmentFieldComponent>(otherUid) ||
            EntityManager.HasComponent<ContainmentFieldGeneratorComponent>(otherUid))
        {
            if (comp.CanBreachContainment)
                args.Cancelled = true;

            return true;
        }

        return false;
    }

#endregion EventHandlers
}
