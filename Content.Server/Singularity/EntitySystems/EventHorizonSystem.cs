using Robust.Shared.Containers;
using Robust.Shared.Timing;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Events;

using Content.Shared.Singularity.Components;
using Content.Shared.Singularity.EntitySystems;

using Content.Server.Ghost.Components;
using Content.Server.Station.Components;
using Content.Server.Singularity.Components;
using Content.Server.Singularity.Events;

namespace Content.Server.Singularity.EntitySystems;

/// <summary>
/// The entity system primarily responsible for managing <see cref="EventHorizonComponent"/>s.
/// Handles their consumption of entities.
/// </summary>
public sealed class EventHorizonSystem : SharedEventHorizonSystem
{
#region Dependencies
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
#endregion Dependencies

    /// <summary>
    /// The maximum number of nested containers an event horizon is allowed to eat through in an attempt to get to the map.
    /// </summary>
    private const int MaxEventHorizonUnnestingIterations = 100;

    /// <summary>
    /// The maximum number of nested containers an immune entity in a container being consumed by an event horizon is allowed to search through before it gives up and just jumps to the map.
    /// </summary>
    private const int MaxEventHorizonDumpSearchIterations = 100;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MapGridComponent, EventHorizonAttemptConsumeEntityEvent>(PreventConsume);
        SubscribeLocalEvent<GhostComponent, EventHorizonAttemptConsumeEntityEvent>(PreventConsume);
        SubscribeLocalEvent<StationDataComponent, EventHorizonAttemptConsumeEntityEvent>(PreventConsume);
        SubscribeLocalEvent<EventHorizonComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<EventHorizonComponent, EntGotInsertedIntoContainerMessage>(OnEventHorizonContained);
        SubscribeLocalEvent<EventHorizonContainedEvent>(OnEventHorizonContained);
        SubscribeLocalEvent<EventHorizonComponent, EventHorizonAttemptConsumeEntityEvent>(OnAnotherEventHorizonAttemptConsumeThisEventHorizon);
        SubscribeLocalEvent<EventHorizonComponent, EventHorizonConsumedEntityEvent>(OnAnotherEventHorizonConsumedThisEventHorizon);
        SubscribeLocalEvent<ContainerManagerComponent, EventHorizonConsumedEntityEvent>(OnContainerConsumed);

        var vvHandle = Vvm.GetTypeHandler<EventHorizonComponent>();
        vvHandle.AddPath(nameof(EventHorizonComponent.TargetConsumePeriod), (_, comp) => comp.TargetConsumePeriod, SetConsumePeriod);
    }

    public override void Shutdown()
    {
        var vvHandle = Vvm.GetTypeHandler<EventHorizonComponent>();
        vvHandle.RemovePath(nameof(EventHorizonComponent.TargetConsumePeriod));

        base.Shutdown();
    }

    /// <summary>
    /// Updates the cooldowns of all event horizons.
    /// If an event horizon are off cooldown this makes it consume everything within range and resets their cooldown.
    /// </summary>
    /// <param name="frameTime">The amount of time that has elapsed since the last cooldown update.</param>
    public override void Update(float frameTime)
    {
        if(!_timing.IsFirstTimePredicted)
            return;

        foreach(var (eventHorizon, xform) in EntityManager.EntityQuery<EventHorizonComponent, TransformComponent>())
        {
            var curTime = _timing.CurTime;
            if (eventHorizon.NextConsumeWaveTime <= curTime)
                Update(eventHorizon.Owner, eventHorizon, xform);
        }
    }

    /// <summary>
    /// Makes an event horizon consume everything nearby and resets the cooldown it for the next automated wave.
    /// </summary>
    /// <param name="uid">The uid of the event horizon consuming everything nearby.</param>
    /// <param name="eventHorizon">The event horizon we want to consume nearby things.</param>
    /// <param name="xform">The transform of the event horizon.</param>
    public void Update(EntityUid uid, EventHorizonComponent? eventHorizon = null, TransformComponent? xform = null)
    {
        if(!Resolve(uid, ref eventHorizon))
            return;

        eventHorizon.LastConsumeWaveTime = _timing.CurTime;
        eventHorizon.NextConsumeWaveTime = eventHorizon.LastConsumeWaveTime + eventHorizon.TargetConsumePeriod;
        if (eventHorizon.BeingConsumedByAnotherEventHorizon)
            return;
        if(!Resolve(uid, ref xform))
            return;

        // Handle singularities some admin smited into a locker.
        if (_containerSystem.TryGetContainingContainer(uid, out var container, transform: xform)
        && !AttemptConsumeEntity(container.Owner, eventHorizon))
        {
            ConsumeEntitiesInContainer(uid, container, eventHorizon, container);
            return;
        }

        if (eventHorizon.Radius > 0.0f)
            ConsumeEverythingInRange(xform.Owner, eventHorizon.Radius, xform, eventHorizon);
    }

#region Consume

#region Consume Entities

    /// <summary>
    /// Makes an event horizon consume a given entity.
    /// </summary>
    /// <param name="uid">The entity to consume.</param>
    /// <param name="eventHorizon">The event horizon consuming the given entity.</param>
    /// <param name="outerContainer">The innermost container of the entity to consume that isn't also being consumed by the event horizon.</param>
    public void ConsumeEntity(EntityUid uid, EventHorizonComponent eventHorizon, IContainer? outerContainer = null)
    {
        EntityManager.QueueDeleteEntity(uid);
        RaiseLocalEvent(eventHorizon.Owner, new EntityConsumedByEventHorizonEvent(uid, eventHorizon, outerContainer));
        RaiseLocalEvent(uid, new EventHorizonConsumedEntityEvent(uid, eventHorizon, outerContainer));
    }

    /// <summary>
    /// Makes an event horizon attempt to consume a given entity.
    /// </summary>
    /// <param name="uid">The entity to attempt to consume.</param>
    /// <param name="eventHorizon">The event horizon attempting to consume the given entity.</param>
    /// <param name="outerContainer">The innermost container of the entity to consume that isn't also being consumed by the event horizon.</param>
    public bool AttemptConsumeEntity(EntityUid uid, EventHorizonComponent eventHorizon, IContainer? outerContainer = null)
    {
        if(!CanConsumeEntity(uid, eventHorizon))
            return false;

        ConsumeEntity(uid, eventHorizon, outerContainer);
        return true;
    }

    /// <summary>
    /// Checks whether an event horizon can consume a given entity.
    /// </summary>
    /// <param name="uid">The entity to check for consumability.</param>
    /// <param name="eventHorizon">The event horizon checking whether it can consume the entity.</param>
    public bool CanConsumeEntity(EntityUid uid, EventHorizonComponent eventHorizon)
    {
        var ev = new EventHorizonAttemptConsumeEntityEvent(uid, eventHorizon);
        RaiseLocalEvent(uid, ev);
        return !ev.Cancelled;
    }

    /// <summary>
    /// Attempts to consume all entities within a given distance of an entity;
    /// Excludes the center entity.
    /// </summary>
    /// <param name="uid">The entity uid in the center of the region to consume all entities within.</param>
    /// <param name="range">The distance of the center entity within which to consume all entities.</param>
    /// <param name="xform">The transform component attached to the center entity.</param>
    /// <param name="eventHorizon">The event horizon component attached to the center entity.</param>
    public void ConsumeEntitiesInRange(EntityUid uid, float range, TransformComponent? xform = null, EventHorizonComponent? eventHorizon = null)
    {
        if(!Resolve(uid, ref xform, ref eventHorizon))
            return;

        foreach(var entity in _lookup.GetEntitiesInRange(xform.MapPosition, range, flags: LookupFlags.Uncontained))
        {
            if (entity == uid)
                continue;

            AttemptConsumeEntity(entity, eventHorizon);
        }
    }

    /// <summary>
    /// Attempts to consume all entities within a container.
    /// Excludes the event horizon itself.
    /// All immune entities within the container will be dumped to a given container or the map/grid if that is impossible.
    /// </summary>
    /// <param name="uid">The uid of the event horizon. The single entity that is immune-by-default.</param>
    /// <param name="container">The container within which to consume all entities.</param>
    /// <param name="eventHorizon">The state of the event horizon.</param>
    /// <param name="outerContainer">The location any immune entities within the container should be dumped to.</param>
    public void ConsumeEntitiesInContainer(EntityUid uid, IContainer container, EventHorizonComponent eventHorizon, IContainer? outerContainer = null) {
        // Removing the immune entities from the container needs to be deferred until after iteration or the iterator raises an error.
        List<EntityUid> immune = new();

        foreach(var entity in container.ContainedEntities)
        {
            if (entity == uid || !AttemptConsumeEntity(entity, eventHorizon, outerContainer))
                immune.Add(entity); // The first check keeps singularities an admin smited into a locker from consuming themselves.
                                    // The second check keeps things that have been rendered immune to singularities from being deleted by a singularity eating their container.
        }

        if (outerContainer == container)
            return; // The container we are intended to drop immune things to is the same container we are consuming everything in
                    //  it's a safe bet that we aren't consuming the container entity so there's no reason to eject anything from this container.

        // We need to get the immune things out of the container because the chances are we are about to eat the container and we don't want them to get deleted despite their immunity.
        foreach(var entity in immune)
        {
            // Attempt to insert immune entities into innermost container at least as outer as outerContainer.
            var target_container = outerContainer;
            while(target_container != null)
            {
                if (target_container.Insert(entity))
                    break;

                _containerSystem.TryGetContainingContainer(target_container.Owner, out target_container);
            }

            // If we couldn't or there was no container to insert into just dump them to the map/grid.
            if (target_container == null)
                Transform(entity).AttachToGridOrMap();
        }
    }

#endregion Consume Entities

#region Consume Tiles

    /// <summary>
    /// Makes an event horizon consume a specific tile on a grid.
    /// </summary>
    /// <param name="tile">The tile to consume.</param>
    /// <param name="eventHorizon">The event horizon which is consuming the tile on the grid.</param>
    public void ConsumeTile(TileRef tile, EventHorizonComponent eventHorizon)
        => ConsumeTiles(new List<(Vector2i, Tile)>(new []{(tile.GridIndices, Tile.Empty)}), _mapMan.GetGrid(tile.GridUid), eventHorizon);

    /// <summary>
    /// Makes an event horizon attempt to consume a specific tile on a grid.
    /// </summary>
    /// <param name="tile">The tile to attempt to consume.</param>
    /// <param name="eventHorizon">The event horizon which is attempting to consume the tile on the grid.</param>
    public void AttemptConsumeTile(TileRef tile, EventHorizonComponent eventHorizon)
        => AttemptConsumeTiles(new TileRef[1]{tile}, _mapMan.GetGrid(tile.GridUid), eventHorizon);

    /// <summary>
    /// Makes an event horizon consume a set of tiles on a grid.
    /// </summary>
    /// <param name="tiles">The tiles to consume.</param>
    /// <param name="grid">The grid hosting the tiles to consume.</param>
    /// <param name="eventHorizon">The event horizon which is consuming the tiles on the grid.</param>
    public void ConsumeTiles(List<(Vector2i, Tile)> tiles, MapGridComponent grid, EventHorizonComponent eventHorizon)
    {
        if (tiles.Count > 0)
            RaiseLocalEvent(eventHorizon.Owner, new TilesConsumedByEventHorizonEvent(tiles, grid, eventHorizon));
            grid.SetTiles(tiles);
    }

    /// <summary>
    /// Makes an event horizon attempt to consume a set of tiles on a grid.
    /// </summary>
    /// <param name="tiles">The tiles to attempt to consume.</param>
    /// <param name="grid">The grid hosting the tiles to attempt to consume.</param>
    /// <param name="eventHorizon">The event horizon which is attempting to consume the tiles on the grid.</param>
    public int AttemptConsumeTiles(IEnumerable<TileRef> tiles, MapGridComponent grid, EventHorizonComponent eventHorizon)
    {
        var toConsume = new List<(Vector2i, Tile)>();
        foreach(var tile in tiles) {
            if (CanConsumeTile(tile, grid, eventHorizon))
                toConsume.Add((tile.GridIndices, Tile.Empty));
        }

        var result = toConsume.Count;
        if (toConsume.Count > 0)
            ConsumeTiles(toConsume, grid, eventHorizon);
        return result;
    }

    /// <summary>
    /// Checks whether an event horizon can consume a given tile.
    /// This is only possible if it can also consume all entities anchored to the tile.
    /// </summary>
    /// <param name="tile">The tile to check for consumability.</param>
    /// <param name="grid">The grid hosting the tile to check.</param>
    /// <param name="eventHorizon">The event horizon which is checking to see if it can consume the tile on the grid.</param>
    public bool CanConsumeTile(TileRef tile, MapGridComponent grid, EventHorizonComponent eventHorizon)
    {
        foreach(var blockingEntity in grid.GetAnchoredEntities(tile.GridIndices))
        {
            if(!CanConsumeEntity(blockingEntity, eventHorizon))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Consumes all tiles within a given distance of an entity.
    /// Some entities are immune to consumption.
    /// </summary>
    /// <param name="uid">The entity uid in the center of the region to consume all tiles within.</param>
    /// <param name="range">The distance of the center entity within which to consume all tiles.</param>
    /// <param name="xform">The transform component attached to the center entity.</param>
    /// <param name="eventHorizon">The event horizon component attached to the center entity.</param>
    public void ConsumeTilesInRange(EntityUid uid, float range, TransformComponent? xform, EventHorizonComponent? eventHorizon)
    {
        if(!Resolve(uid, ref xform) || !Resolve(uid, ref eventHorizon))
            return;

        var mapPos = xform.MapPosition;
        var box = Box2.CenteredAround(mapPos.Position, new Vector2(range, range));
        var circle = new Circle(mapPos.Position, range);
        foreach(var grid in _mapMan.FindGridsIntersecting(mapPos.MapId, box))
        {
            AttemptConsumeTiles(grid.GetTilesIntersecting(circle), grid, eventHorizon);
        }
    }

#endregion Consume Tiles

    /// <summary>
    /// Consumes most entities and tiles within a given distance of an entity.
    /// Some entities are immune to consumption.
    /// </summary>
    /// <param name="uid">The entity uid in the center of the region to consume everything within.</param>
    /// <param name="range">The distance of the center entity within which to consume everything.</param>
    /// <param name="xform">The transform component attached to the center entity.</param>
    /// <param name="eventHorizon">The event horizon component attached to the center entity.</param>
    public void ConsumeEverythingInRange(EntityUid uid, float range, TransformComponent? xform = null, EventHorizonComponent? eventHorizon = null)
    {
        if(!Resolve(uid, ref xform, ref eventHorizon))
            return;

        ConsumeEntitiesInRange(uid, range, xform, eventHorizon);
        ConsumeTilesInRange(uid, range, xform, eventHorizon);
    }

#endregion Consume

    #region Getters/Setters

    /// <summary>
    /// Sets how often an event horizon will scan for overlapping entities to consume.
    /// The value is specifically how long the subsystem should wait between scans.
    /// If the new scanning period would have already prompted a scan given the previous scan time one is prompted immediately.
    /// </summary>
    /// <param name="uid">The uid of the event horizon to set the consume wave period for.</param>
    /// <param name="value">The amount of time that this subsystem should wait between scans.</param>
    /// <param name="eventHorizon">The state of the event horizon to set the consume wave period for.</param>
    public void SetConsumePeriod(EntityUid uid, TimeSpan value, EventHorizonComponent? eventHorizon = null)
    {
        if(!Resolve(uid, ref eventHorizon))
            return;

        if (MathHelper.CloseTo(eventHorizon.TargetConsumePeriod.TotalSeconds, value.TotalSeconds))
            return;

        eventHorizon.TargetConsumePeriod = value;
        eventHorizon.NextConsumeWaveTime = eventHorizon.LastConsumeWaveTime + eventHorizon.TargetConsumePeriod;

        var curTime = _timing.CurTime;
        if (eventHorizon.NextConsumeWaveTime < curTime)
            Update(uid, eventHorizon);
    }

    #endregion Getters/Setters

#region Event Handlers

    /// <summary>
    /// Prevents a singularity from colliding with anything it is incapable of consuming.
    /// </summary>
    /// <param name="uid">The event horizon entity that is trying to collide with something.</param>
    /// <param name="comp">The event horizon that is trying to collide with something.</param>
    /// <param name="args">The event arguments.</param>
    protected override sealed bool PreventCollide(EntityUid uid, EventHorizonComponent comp, ref PreventCollideEvent args)
    {
        if (base.PreventCollide(uid, comp, ref args) || args.Cancelled)
            return true;

        args.Cancelled = !CanConsumeEntity(args.BodyB.Owner, (EventHorizonComponent)comp);
        return false;
    }

    /// <summary>
    /// A generic event handler that prevents singularities from consuming entities with a component of a given type if registered.
    /// </summary>
    /// <param name="uid">The entity the singularity is trying to eat.</param>
    /// <param name="comp">The component the singularity is trying to eat.</param>
    /// <param name="args">The event arguments.</param>
    public void PreventConsume<TComp>(EntityUid uid, TComp comp, EventHorizonAttemptConsumeEntityEvent args)
    {
        if(!args.Cancelled)
            args.Cancel();
    }

    /// <summary>
    /// A generic event handler that prevents singularities from breaching containment.
    /// In this case 'breaching containment' means consuming an entity with a component of the given type unless the event horizon is set to breach containment anyway.
    /// </summary>
    /// <param name="uid">The entity the singularity is trying to eat.</param>
    /// <param name="comp">The component the singularity is trying to eat.</param>
    /// <param name="args">The event arguments.</param>
    public void PreventBreach<TComp>(EntityUid uid, TComp comp, EventHorizonAttemptConsumeEntityEvent args)
    {
        if (args.Cancelled)
            return;
        if(!args.EventHorizon.CanBreachContainment)
            PreventConsume(uid, comp, args);
    }

    /// <summary>
    /// Handles event horizons consuming any entities they bump into.
    /// The event horizon will not consume any entities if it itself has been consumed by an event horizon.
    /// </summary>
    /// <param name="uid">The event horizon entity.</param>
    /// <param name="comp">The event horizon.</param>
    /// <param name="args">The event arguments.</param>
    private void OnStartCollide(EntityUid uid, EventHorizonComponent comp, ref StartCollideEvent args)
    {
        if (comp.BeingConsumedByAnotherEventHorizon)
            return;
        if (args.OurFixture.ID != comp.HorizonFixtureId)
            return;

        AttemptConsumeEntity(args.OtherFixture.Body.Owner, comp);
    }

    /// <summary>
    /// Prevents two event horizons from annihilating one another.
    /// Specifically prevents event horizons from consuming themselves.
    /// Also ensures that if this event horizon has already been consumed by another event horizon it cannot be consumed again.
    /// </summary>
    /// <param name="uid">The event horizon entity.</param>
    /// <param name="comp">The event horizon.</param>
    /// <param name="args">The event arguments.</param>
    private void OnAnotherEventHorizonAttemptConsumeThisEventHorizon(EntityUid uid, EventHorizonComponent comp, EventHorizonAttemptConsumeEntityEvent args)
    {
        if(!args.Cancelled && (args.EventHorizon == comp || comp.BeingConsumedByAnotherEventHorizon))
            args.Cancel();
    }

    /// <summary>
    /// Prevents two singularities from annihilating one another.
    /// Specifically ensures if this event horizon is consumed by another event horizon it knows that it has been consumed.
    /// </summary>
    /// <param name="uid">The event horizon entity.</param>
    /// <param name="comp">The event horizon.</param>
    /// <param name="args">The event arguments.</param>
    private void OnAnotherEventHorizonConsumedThisEventHorizon(EntityUid uid, EventHorizonComponent comp, EventHorizonConsumedEntityEvent args)
    {
        comp.BeingConsumedByAnotherEventHorizon = true;
    }

    /// <summary>
    /// Handles event horizons deciding to escape containers they are inserted into.
    /// Delegates the actual escape to <see cref="EventHorizonSystem.OnEventHorizonContained(EventHorizonContainedEvent)"> on a delay.
    /// This ensures that the escape is handled after all other handlers for the insertion event and satisfies the assertion that
    ///     the inserted entity SHALL be inside of the specified container after all handles to the entity event
    ///     <see cref="EntGotInsertedIntoContainerMessage"> are processed.
    /// </summary>
    /// <param name="uid">The uid of the event horizon.</param>]
    /// <param name="comp">The state of the event horizon.</param>]
    /// <param name="args">The arguments of the insertion.</param>]
    private void OnEventHorizonContained(EntityUid uid, EventHorizonComponent comp, EntGotInsertedIntoContainerMessage args) {
        // Delegates processing an event until all queued events have been processed.
        // As of 1:44 AM, Sunday, Dec. 4, 2022 this is the one use for this in the codebase.
        QueueLocalEvent(new EventHorizonContainedEvent(uid, comp, args));
    }

    /// <summary>
    /// Handles event horizons attempting to escape containers they have been inserted into.
    /// If the event horizon has not been consumed by another event horizon this handles making the event horizon consume the containing
    ///     container and drop the the next innermost contaning container.
    /// This loops until the event horizon has escaped to the map or wound up in an indestructible container.
    /// </summary>
    /// <param name="args">The arguments for this event.</param>]
    private void OnEventHorizonContained(EventHorizonContainedEvent args) {
        var uid = args.Entity;
        var comp = args.EventHorizon;
        if (!EntityManager.EntityExists(uid))
            return;
        if (comp.BeingConsumedByAnotherEventHorizon)
            return;

        var containerEntity = args.Args.Container.Owner;
        if(!(EntityManager.EntityExists(containerEntity) && AttemptConsumeEntity(containerEntity, comp))) {
            ConsumeEntitiesInContainer(uid, args.Args.Container, comp, args.Args.Container);
        }
    }

    /// <summary>
    /// Recursively consumes all entities within a container that is consumed by the singularity.
    /// If an entity within a consumed container cannot be consumed itself it is removed from the container.
    /// </summary>
    /// <param name="uid">The uid of the container being consumed.</param>
    /// <param name="comp">The state of the container being consumed.</param>
    /// <param name="args">The event arguments.</param>
    private void OnContainerConsumed(EntityUid uid, ContainerManagerComponent comp, EventHorizonConsumedEntityEvent args)
    {
        var drop_container = args.Container;
        if (drop_container is null)
            _containerSystem.TryGetContainingContainer(uid, out drop_container);

        foreach(var container in comp.GetAllContainers())
        {
            ConsumeEntitiesInContainer(args.EventHorizon.Owner, container, args.EventHorizon, drop_container);
        }
    }
#endregion Event Handlers
}
