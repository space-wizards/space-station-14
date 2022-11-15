using Robust.Shared.Map;
using Robust.Shared.Physics.Events;
using Robust.Shared.Containers;

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
    [Dependency] private readonly IMapManager _mapMan = default!;
#endregion Dependencies
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MapGridComponent, EventHorizonAttemptConsumeEntityEvent>(PreventConsume);
        SubscribeLocalEvent<GhostComponent, EventHorizonAttemptConsumeEntityEvent>(PreventConsume);
        SubscribeLocalEvent<StationDataComponent, EventHorizonAttemptConsumeEntityEvent>(PreventConsume);
        SubscribeLocalEvent<EventHorizonComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<EventHorizonComponent, EventHorizonAttemptConsumeEntityEvent>(OnAnotherEventHorizonAttemptConsumeThisEventHorizon);
        SubscribeLocalEvent<EventHorizonComponent, EventHorizonConsumedEntityEvent>(OnAnotherEventHorizonConsumedThisEventHorizon);
        SubscribeLocalEvent<ContainerManagerComponent, EventHorizonConsumedEntityEvent>(OnContainerConsumed);
    }

    /// <summary>
    /// Updates the cooldowns of all event horizons.
    /// If an event horizon are off cooldown this makes it consume everything within range and resets their cooldown.
    /// </summary>
    /// <param name="frameTime">The amount of time that has elapsed since the last cooldown update.</param>]
    public override void Update(float frameTime)
    {
        foreach(var (eventHorizon, xform) in EntityManager.EntityQuery<EventHorizonComponent, TransformComponent>())
        {
            if ((eventHorizon.TimeSinceLastConsumeWave += frameTime) > eventHorizon.ConsumePeriod)
                Update(eventHorizon, xform);
        }
    }

    /// <summary>
    /// Makes an event horizon consume everything nearby and resets the cooldown it for the next automated wave.
    /// </summary>
    /// <param name="eventHorizon">The event horizon we want to consume nearby things.</param>
    /// <param name="xform">The transform of the event horizon.</param>
    public void Update(EventHorizonComponent eventHorizon, TransformComponent? xform)
    {
        eventHorizon.TimeSinceLastConsumeWave = 0.0f;
        if(!Resolve(eventHorizon.Owner, ref xform))
            return;
        if (eventHorizon.Radius < 0.0f || eventHorizon.BeingConsumedByAnotherEventHorizon)
            return;

        ConsumeEverythingInRange(xform.Owner, eventHorizon.Radius, xform, eventHorizon);
    }

#region Consume

#region Consume Entities

    /// <summary>
    /// Makes an event horizon consume a given entity.
    /// </summary>
    /// <param name="uid">The entity to consume.</param>
    /// <param name="eventHorizon">The event horizon consuming the given entity.</param>
    public void ConsumeEntity(EntityUid uid, EventHorizonComponent eventHorizon)
    {
        RaiseLocalEvent(eventHorizon.Owner, new EntityConsumedByEventHorizonEvent(uid, eventHorizon));
        RaiseLocalEvent(uid, new EventHorizonConsumedEntityEvent(uid, eventHorizon));
        EntityManager.QueueDeleteEntity(uid);
    }

    /// <summary>
    /// Makes an event horizon attempt to consume a given entity.
    /// </summary>
    /// <param name="uid">The entity to attempt to consume.</param>
    /// <param name="eventHorizon">The event horizon attempting to consume the given entity.</param>
    public bool AttemptConsumeEntity(EntityUid uid, EventHorizonComponent eventHorizon)
    {
        if(!CanConsumeEntity(uid, eventHorizon))
            return false;

        ConsumeEntity(uid, eventHorizon);
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
    public void ConsumeEntitiesInRange(EntityUid uid, float range, TransformComponent? xform, EventHorizonComponent? eventHorizon)
    {
        if(!Resolve(uid, ref xform) || !Resolve(uid, ref eventHorizon))
            return;

        foreach(var entity in _lookup.GetEntitiesInRange(xform.MapPosition, range, flags: LookupFlags.Uncontained))
        {
            if (entity == uid)
                continue;

            AttemptConsumeEntity(entity, eventHorizon);
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
    public void ConsumeTiles(List<(Vector2i, Tile)> tiles, IMapGrid grid, EventHorizonComponent eventHorizon)
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
    public int AttemptConsumeTiles(IEnumerable<TileRef> tiles, IMapGrid grid, EventHorizonComponent eventHorizon)
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
    public bool CanConsumeTile(TileRef tile, IMapGrid grid, EventHorizonComponent eventHorizon)
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
    public void ConsumeEverythingInRange(EntityUid uid, float range, TransformComponent? xform, EventHorizonComponent? eventHorizon)
    {
        if(!Resolve(uid, ref xform) || !Resolve(uid, ref eventHorizon))
            return;

        ConsumeEntitiesInRange(uid, range, xform, eventHorizon);
        ConsumeTilesInRange(uid, range, xform, eventHorizon);
    }

#endregion Consume

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
    /// Recursively consumes all entities within a container that is consumed by the singularity.
    /// If an entity within a consumed container cannot be consumed itself it is removed from the container.
    /// </summary>
    /// <param name="uid">The uid of the container being consumed.</param>
    /// <param name="comp">The state of the container being consumed.</param>
    /// <param name="args">The event arguments.</param>
    private void OnContainerConsumed(EntityUid uid, ContainerManagerComponent comp, EventHorizonConsumedEntityEvent args)
    {
        foreach(var container in comp.GetAllContainers())
        {
            foreach(var contained in container.ContainedEntities)
            {
                if(!AttemptConsumeEntity(contained, args.EventHorizon))
                {
                    // Forcefully removes this entity from whatever container is has and places it on the ground.
                    // We can't use container.Remove because that can place us in a parent container
                    //  and we happen to be consuming all parent containers. It could also fail which
                    //  doesn't make much sense when the containing entity is ceasing to exist and
                    //  could cause entities that are immune to singularities to be deleted by
                    //  singularities consuming their container.
                    Transform(contained).AttachToGridOrMap();
                }
            }
        }
    }
#endregion Event Handlers
}
