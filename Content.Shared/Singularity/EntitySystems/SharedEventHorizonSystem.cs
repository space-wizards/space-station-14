using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

using Content.Shared.Ghost;
using Content.Shared.Singularity.Components;

namespace Content.Shared.Singularity.EntitySystems;

/// <summary>
/// The entity system primarily responsible for managing <see cref="SharedEventHorizonComponent"/>s.
/// </summary>
public abstract class SharedEventHorizonSystem : EntitySystem
{
#region Dependencies
    [Dependency] private readonly FixtureSystem _fixtures = default!;
    [Dependency] private readonly IViewVariablesManager _vvm = default!;
#endregion Dependencies
    public override void Initialize()
    {
        base.Initialize();

        // Allows for predicted collisions with singularities.
        SubscribeLocalEvent<SharedEventHorizonComponent, ComponentStartup>(OnEventHorizonStartup);
        SubscribeLocalEvent<SharedEventHorizonComponent, PreventCollideEvent>(OnPreventCollide);

        var vvHandle = _vvm.GetTypeHandler<SharedEventHorizonComponent>();
        vvHandle.AddPath(nameof(SharedEventHorizonComponent.Radius), (_, comp) => comp.Radius, SetRadius);
        vvHandle.AddPath(nameof(SharedEventHorizonComponent.CanBreachContainment), (_, comp) => comp.CanBreachContainment, SetCanBreachContainment);
        vvHandle.AddPath(nameof(SharedEventHorizonComponent.HorizonFixtureId), (_, comp) => comp.HorizonFixtureId, SetHorizonFixtureId);
    }

    public override void Shutdown()
    {
        var vvHandle = _vvm.GetTypeHandler<SharedEventHorizonComponent>();
        vvHandle.RemovePath(nameof(SharedEventHorizonComponent.Radius));
        vvHandle.RemovePath(nameof(SharedEventHorizonComponent.CanBreachContainment));
        vvHandle.RemovePath(nameof(SharedEventHorizonComponent.HorizonFixtureId));

        base.Shutdown();
    }

#region Getters/Setters

    /// <summary>
    /// Setter for <see cref="SharedEventHorizonComponent.Radius"/>
    /// May also update the fixture associated with the event horizon.
    /// </summary>
    /// <param name="eventHorizon">The event horizon to change the radius of.</param>
    /// <param name="value">The new radius of the event horizon.</param>
    /// <param name="updateFixture">Whether to update the associated fixture upon changing the radius of the event horizon.</param>
    public void SetRadius(SharedEventHorizonComponent eventHorizon, float value, bool updateFixture = true)
    {
        var oldValue = eventHorizon.Radius;
        if (value == oldValue)
            return;

        eventHorizon.Radius = value;
        eventHorizon.Dirty();
        if (updateFixture)
            UpdateEventHorizonFixture(eventHorizon);
    }

    /// <summary>
    /// Setter for <see cref="SharedEventHorizonComponent.CanBreachContainment"/>
    /// May also update the fixture associated with the event horizon.
    /// </summary>
    /// <param name="eventHorizon">The event horizon to make (in)capable of breaching containment.</param>
    /// <param name="value">Whether the event horizon should be able to breach containment.</param>
    /// <param name="updateFixture">Whether to update the associated fixture upon changing whether the event horizon can breach containment.</param>
    public void SetCanBreachContainment(SharedEventHorizonComponent eventHorizon, bool value, bool updateFixture = true)
    {
        var oldValue = eventHorizon.CanBreachContainment;
        if (value == oldValue)
            return;

        eventHorizon.CanBreachContainment = value;
        eventHorizon.Dirty();
        if (updateFixture)
            UpdateEventHorizonFixture(eventHorizon);
    }

    /// <summary>
    /// Setter for <see cref="SharedEventHorizonComponent.HorizonFixtureId"/>
    /// May also update the fixture associated with the event horizon.
    /// </summary>
    /// <param name="eventHorizon">The event horizon with the fixture ID to change.</param>
    /// <param name="value">The new fixture ID to associate the event horizon with.</param>
    /// <param name="updateFixture">Whether to update the associated fixture upon changing whether the event horizon can breach containment.</param>
    public void SetHorizonFixtureId(SharedEventHorizonComponent eventHorizon, string? value, bool updateFixture = true)
    {
        var oldValue = eventHorizon.HorizonFixtureId;
        if (value == oldValue)
            return;

        eventHorizon.HorizonFixtureId = value;
        eventHorizon.Dirty();
        if (updateFixture)
            UpdateEventHorizonFixture(eventHorizon);
    }

    /// <summary>
    /// Updates the state of the fixture associated with the event horizon.
    /// </summary>
    /// <param name="eventHorizon">The event horizon component associated with the fixture to update.</param>
    /// <param name="fixtures">The physics component containing the fixture to update.</param>
    public void UpdateEventHorizonFixture(SharedEventHorizonComponent eventHorizon, PhysicsComponent? fixtures = null)
    {
        var fixtureId = eventHorizon.HorizonFixtureId;
        if (fixtureId == null || !Resolve(eventHorizon.Owner, ref fixtures, logMissing: false))
            return;

        var fixture = _fixtures.GetFixtureOrNull(fixtures, fixtureId);
        if (fixture == null)
            return;

        var shape = (PhysShapeCircle)fixture.Shape;
        shape.Radius = eventHorizon.Radius;
        fixture.Hard = !eventHorizon.CanBreachContainment;
        fixtures.Dirty();
    }
#region VV
    /// <summary>
    /// VV Setter for <see cref="SharedEventHorizonComponent.Radius"/>
    /// Will also update the fixture associated with the event horizon if it exists.
    /// </summary>
    /// <param name="uid">The entity with the event horizon that is being modified.</param>
    /// <param name="value">The new radius of the event horizon.</param>
    /// <param name="comp">The event horizon to set the radius of.</param>
    private void SetRadius(EntityUid uid, float value, SharedEventHorizonComponent? comp)
    {
        if (Resolve(uid, ref comp))
            SetRadius(comp, value);
    }

    /// <summary>
    /// VV Setter for <see cref="SharedEventHorizonComponent.CanBreachContainment"/>
    /// Will also update the fixture associated with the event horizon if it exists.
    /// </summary>
    /// <param name="uid">The entity with the event horizon that is being modified.</param>
    /// <param name="value">Whether the event horizon should be able to breach containment.</param>
    /// <param name="comp">The event horizon to make (in)capable of breaching containment.</param>
    private void SetCanBreachContainment(EntityUid uid, bool value, SharedEventHorizonComponent? comp)
    {
        if (Resolve(uid, ref comp))
            SetCanBreachContainment(comp, value);
    }

    /// <summary>
    /// VV Setter for <see cref="SharedEventHorizonComponent.HorizonFixtureId"/>
    /// Will also update the fixture associated with the event horizon if it exists.
    /// </summary>
    /// <param name="uid">The entity with the event horizon that is being modified.</param>
    /// <param name="value">The new fixture ID to associate the event horizon with.</param>
    /// <param name="comp">The event horizon to change the associated fixture of.</param>
    private void SetHorizonFixtureId(EntityUid uid, string? value, SharedEventHorizonComponent? comp)
    {
        if (Resolve(uid, ref comp))
            SetHorizonFixtureId(comp, value);
    }
#endregion VV
#endregion Getters/Setters


#region EventHandlers

    /// <summary>
    /// Syncs the state of the fixture associated with the event horizon upon startup.
    /// </summary>
    /// <param name="uid">The entity that has just gained an event horizon component.</param>
    /// <param name="comp">The event horizon component that is starting up.</param>
    /// <param name="args">The event arguments.</param>
    private void OnEventHorizonStartup(EntityUid uid, SharedEventHorizonComponent comp, ComponentStartup args)
    {
        UpdateEventHorizonFixture(comp);
    }

    /// <summary>
    /// Prevents the event horizon from colliding with anything it cannot consume.
    /// Most notably map grids and ghosts.
    /// Also makes event horizons phase through containment if it can breach.
    /// </summary>
    /// <param name="uid">The entity that is trying to collide with another entity.</param>
    /// <param name="comp">The event horizon of the former.</param>
    /// <param name="args">The event arguments.</param>
    private void OnPreventCollide(EntityUid uid, SharedEventHorizonComponent comp, ref PreventCollideEvent args)
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
    protected virtual bool PreventCollide(EntityUid uid, SharedEventHorizonComponent comp, ref PreventCollideEvent args)
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
