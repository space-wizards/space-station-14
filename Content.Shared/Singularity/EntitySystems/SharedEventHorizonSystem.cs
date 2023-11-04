using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

using Content.Shared.Ghost;
using Content.Shared.Singularity.Components;
using Robust.Shared.Physics;

namespace Content.Shared.Singularity.EntitySystems;

/// <summary>
/// The entity system primarily responsible for managing <see cref="EventHorizonComponent"/>s.
/// </summary>
public abstract class SharedEventHorizonSystem : EntitySystem
{

    [Dependency] private readonly FixtureSystem _fixtures = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] protected readonly IViewVariablesManager Vvm = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Allows for predicted collisions with singularities.
        SubscribeLocalEvent<EventHorizonComponent, ComponentStartup>(OnEventHorizonStartup);
        SubscribeLocalEvent<EventHorizonComponent, PreventCollideEvent>(OnPreventCollide);

        var vvHandle = Vvm.GetTypeHandler<EventHorizonComponent>();
        vvHandle.AddPath(nameof(EventHorizonComponent.Radius), (_, comp) => comp.Radius, (uid, value, comp) => SetRadius(uid, value, eventHorizon: comp));
        vvHandle.AddPath(nameof(EventHorizonComponent.CanBreachContainment), (_, comp) => comp.CanBreachContainment, (uid, value, comp) => SetCanBreachContainment(uid, value, eventHorizon: comp));
        vvHandle.AddPath(nameof(EventHorizonComponent.ColliderFixtureId), (_, comp) => comp.ColliderFixtureId, (uid, value, comp) => SetColliderFixtureId(uid, value, eventHorizon: comp));
        vvHandle.AddPath(nameof(EventHorizonComponent.ConsumerFixtureId), (_, comp) => comp.ConsumerFixtureId, (uid, value, comp) => SetConsumerFixtureId(uid, value, eventHorizon: comp));
    }

    public override void Shutdown()
    {
        var vvHandle = Vvm.GetTypeHandler<EventHorizonComponent>();
        vvHandle.RemovePath(nameof(EventHorizonComponent.Radius));
        vvHandle.RemovePath(nameof(EventHorizonComponent.CanBreachContainment));
        vvHandle.RemovePath(nameof(EventHorizonComponent.ColliderFixtureId));
        vvHandle.RemovePath(nameof(EventHorizonComponent.ConsumerFixtureId));

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
        if (!Resolve(uid, ref eventHorizon))
            return;

        var oldValue = eventHorizon.Radius;
        if (value == oldValue)
            return;

        eventHorizon.Radius = value;
        Dirty(eventHorizon);
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
        if (!Resolve(uid, ref eventHorizon))
            return;

        var oldValue = eventHorizon.CanBreachContainment;
        if (value == oldValue)
            return;

        eventHorizon.CanBreachContainment = value;
        Dirty(eventHorizon);
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
    public void SetColliderFixtureId(EntityUid uid, string? value, bool updateFixture = true, EventHorizonComponent? eventHorizon = null)
    {
        if (!Resolve(uid, ref eventHorizon))
            return;

        var oldValue = eventHorizon.ColliderFixtureId;
        if (value == oldValue)
            return;

        eventHorizon.ColliderFixtureId = value;
        Dirty(eventHorizon);
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
    public void SetConsumerFixtureId(EntityUid uid, string? value, bool updateFixture = true, EventHorizonComponent? eventHorizon = null)
    {
        if (!Resolve(uid, ref eventHorizon))
            return;

        var oldValue = eventHorizon.ConsumerFixtureId;
        if (value == oldValue)
            return;

        eventHorizon.ConsumerFixtureId = value;
        Dirty(eventHorizon);
        if (updateFixture)
            UpdateEventHorizonFixture(uid, eventHorizon: eventHorizon);
    }

    /// <summary>
    /// Updates the state of the fixture associated with the event horizon.
    /// </summary>
    /// <param name="uid">The uid of the event horizon associated with the fixture to update.</param>
    /// <param name="fixtures">The fixture manager component containing the fixture to update.</param>
    /// <param name="eventHorizon">The state of the event horizon associated with the fixture to update.</param>
    public void UpdateEventHorizonFixture(EntityUid uid, FixturesComponent? fixtures = null, EventHorizonComponent? eventHorizon = null)
    {
        if (!Resolve(uid, ref eventHorizon))
            return;

        var consumerId = eventHorizon.ConsumerFixtureId;
        var colliderId = eventHorizon.ColliderFixtureId;
        if (consumerId == null || colliderId == null
        || !Resolve(uid, ref fixtures, logMissing: false))
            return;

        // Update both fixtures the event horizon is associated with:
        var consumer = _fixtures.GetFixtureOrNull(uid, consumerId, fixtures);
        if (consumer != null)
        {
            _physics.SetRadius(uid, consumerId, consumer, consumer.Shape, eventHorizon.Radius, fixtures);
            _physics.SetHard(uid, consumer, false, fixtures);
        }

        var collider = _fixtures.GetFixtureOrNull(uid, colliderId, fixtures);
        if (collider != null)
        {
            _physics.SetRadius(uid, colliderId, collider, collider.Shape, eventHorizon.Radius, fixtures);
            _physics.SetHard(uid, collider, true, fixtures);
        }

        EntityManager.Dirty(uid, fixtures);
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
        if (!args.Cancelled)
            PreventCollide(uid, comp, ref args);
    }

    /// <summary>
    /// The actual, functional part of SharedEventHorizonSystem.OnPreventCollide.
    /// The return value allows for overrides to early return if the base successfully handles collision prevention.
    /// </summary>
    /// <param name="uid">The entity that is trying to collide with another entity.</param>
    /// <param name="comp">The event horizon of the former.</param>
    /// <param name="args">The event arguments.</param>
    /// <returns>A bool indicating whether the collision prevention has been handled.</returns>
    protected virtual bool PreventCollide(EntityUid uid, EventHorizonComponent comp, ref PreventCollideEvent args)
    {
        var otherUid = args.OtherEntity;

        // For prediction reasons always want the client to ignore these.
        if (HasComp<MapGridComponent>(otherUid) ||
            HasComp<GhostComponent>(otherUid))
        {
            args.Cancelled = true;
            return true;
        }

        // If we can, breach containment
        // otherwise, check if it's containment and just keep the collision
        if (HasComp<ContainmentFieldComponent>(otherUid) ||
            HasComp<ContainmentFieldGeneratorComponent>(otherUid))
        {
            if (comp.CanBreachContainment)
                args.Cancelled = true;

            return true;
        }

        return false;
    }

    #endregion EventHandlers
}
