using System.Numerics;
using Content.Server.Administration.Logs;
using Content.Server.Singularity.Events;
using Content.Server.Station.Components;
using Content.Shared.Database;
using Content.Shared.Ghost;
using Content.Shared.Mind.Components;
using Content.Shared.Singularity.Components;
using Content.Shared.Singularity.EntitySystems;
using Content.Shared.Tag;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

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
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _xformSystem = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    #endregion Dependencies

    private EntityQuery<PhysicsComponent> _physicsQuery;

    public override void Initialize()
    {
        base.Initialize();

        _physicsQuery = GetEntityQuery<PhysicsComponent>();

        SubscribeLocalEvent<MapGridComponent, EventHorizonAttemptConsumeEntityEvent>(PreventConsume);
        SubscribeLocalEvent<StationDataComponent, EventHorizonAttemptConsumeEntityEvent>(PreventConsume);
        SubscribeLocalEvent<EventHorizonComponent, MapInitEvent>(OnHorizonMapInit);
        SubscribeLocalEvent<EventHorizonComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<EventHorizonComponent, EntGotInsertedIntoContainerMessage>(OnEventHorizonContained);
        SubscribeLocalEvent<EventHorizonContainedEvent>(OnEventHorizonContained);
        SubscribeLocalEvent<EventHorizonComponent, EventHorizonAttemptConsumeEntityEvent>(OnAnotherEventHorizonAttemptConsumeThisEventHorizon);
        SubscribeLocalEvent<EventHorizonComponent, EventHorizonConsumedEntityEvent>(OnAnotherEventHorizonConsumedThisEventHorizon);
        SubscribeLocalEvent<ContainerManagerComponent, EventHorizonConsumedEntityEvent>(OnContainerConsumed);

        var vvHandle = Vvm.GetTypeHandler<EventHorizonComponent>();
        vvHandle.AddPath(nameof(EventHorizonComponent.TargetConsumePeriod), (_, comp) => comp.TargetConsumePeriod, SetConsumePeriod);
    }

    private void OnHorizonMapInit(EntityUid uid, EventHorizonComponent component, MapInitEvent args)
    {
        component.NextConsumeWaveTime = _timing.CurTime;
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
    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<EventHorizonComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var eventHorizon, out var xform))
        {
            var curTime = _timing.CurTime;
            if (eventHorizon.NextConsumeWaveTime <= curTime)
                Update(uid, eventHorizon, xform);
        }
    }

    /// <summary>
    /// Makes an event horizon consume everything nearby and resets the cooldown it for the next automated wave.
    /// </summary>
    public void Update(EntityUid uid, EventHorizonComponent? eventHorizon = null, TransformComponent? xform = null)
    {
        if (!Resolve(uid, ref eventHorizon))
            return;

        eventHorizon.NextConsumeWaveTime += eventHorizon.TargetConsumePeriod;
        if (eventHorizon.BeingConsumedByAnotherEventHorizon)
            return;

        if (!Resolve(uid, ref xform))
            return;

        // Handle singularities some admin smited into a locker.
        if (_containerSystem.TryGetContainingContainer((uid, xform, null), out var container)
        && !AttemptConsumeEntity(uid, container.Owner, eventHorizon))
        {
            // Locker is indestructible. Consume everything else in the locker instead of magically teleporting out.
            ConsumeEntitiesInContainer(uid, container, eventHorizon, container);
            return;
        }

        if (eventHorizon.Radius > 0.0f)
            ConsumeEverythingInRange(uid, eventHorizon.Radius, xform, eventHorizon);
    }

    #region Consume

    #region Consume Entities

    /// <summary>
    /// Makes an event horizon consume a given entity.
    /// </summary>
    public void ConsumeEntity(EntityUid hungry, EntityUid morsel, EventHorizonComponent eventHorizon, BaseContainer? outerContainer = null)
    {
        if (EntityManager.IsQueuedForDeletion(morsel)) // already handled, and we're substepping
            return;

        if (HasComp<MindContainerComponent>(morsel)
            || _tagSystem.HasTag(morsel, "HighRiskItem")
            || HasComp<ContainmentFieldGeneratorComponent>(morsel))
        {
            _adminLogger.Add(LogType.EntityDelete, LogImpact.High, $"{ToPrettyString(morsel):player} entered the event horizon of {ToPrettyString(hungry)} and was deleted");
        }

        EntityManager.QueueDeleteEntity(morsel);
        var evSelf = new EntityConsumedByEventHorizonEvent(morsel, hungry, eventHorizon, outerContainer);
        var evEaten = new EventHorizonConsumedEntityEvent(morsel, hungry, eventHorizon, outerContainer);
        RaiseLocalEvent(hungry, ref evSelf);
        RaiseLocalEvent(morsel, ref evEaten);
    }

    /// <summary>
    /// Makes an event horizon attempt to consume a given entity.
    /// </summary>
    public bool AttemptConsumeEntity(EntityUid hungry, EntityUid morsel, EventHorizonComponent eventHorizon, BaseContainer? outerContainer = null)
    {
        if (!CanConsumeEntity(hungry, morsel, eventHorizon))
            return false;

        ConsumeEntity(hungry, morsel, eventHorizon, outerContainer);
        return true;
    }

    /// <summary>
    /// Checks whether an event horizon can consume a given entity.
    /// </summary>
    public bool CanConsumeEntity(EntityUid hungry, EntityUid uid, EventHorizonComponent eventHorizon)
    {
        var ev = new EventHorizonAttemptConsumeEntityEvent(uid, hungry, eventHorizon);
        RaiseLocalEvent(uid, ref ev);
        return !ev.Cancelled;
    }

    /// <summary>
    /// Attempts to consume all entities within a given distance of an entity;
    /// Excludes the center entity.
    /// </summary>
    public void ConsumeEntitiesInRange(EntityUid uid, float range, PhysicsComponent? body = null, EventHorizonComponent? eventHorizon = null)
    {
        if (!Resolve(uid, ref body, ref eventHorizon))
            return;

        // TODO: Should be sundries + static-sundries but apparently this is load-bearing for SpawnAndDeleteAllEntitiesInTheSameSpot so go figure.
        foreach (var entity in _lookup.GetEntitiesInRange(uid, range, flags: LookupFlags.Uncontained))
        {
            if (entity == uid)
                continue;

            // See TODO above
            if (_physicsQuery.TryComp(entity, out var otherBody) && !_physics.IsHardCollidable((uid, null, body), (entity, null, otherBody)))
                continue;

            AttemptConsumeEntity(uid, entity, eventHorizon);
        }
    }

    /// <summary>
    /// Attempts to consume all entities within a container.
    /// Excludes the event horizon itself.
    /// All immune entities within the container will be dumped to a given container or the map/grid if that is impossible.
    /// </summary>
    public void ConsumeEntitiesInContainer(EntityUid hungry, BaseContainer container, EventHorizonComponent eventHorizon, BaseContainer? outerContainer = null)
    {
        // Removing the immune entities from the container needs to be deferred until after iteration or the iterator raises an error.
        List<EntityUid> immune = new();

        foreach (var entity in container.ContainedEntities)
        {
            if (entity == hungry || !AttemptConsumeEntity(hungry, entity, eventHorizon, outerContainer))
                immune.Add(entity); // The first check keeps singularities an admin smited into a locker from consuming themselves.
                                    // The second check keeps things that have been rendered immune to singularities from being deleted by a singularity eating their container.
        }

        if (outerContainer == container || immune.Count <= 0)
            return; // The container we are intended to drop immune things to is the same container we are consuming everything in
                    //  it's a safe bet that we aren't consuming the container entity so there's no reason to eject anything from this container.

        // We need to get the immune things out of the container because the chances are we are about to eat the container and we don't want them to get deleted despite their immunity.
        foreach (var entity in immune)
        {
            // Attempt to insert immune entities into innermost container at least as outer as outerContainer.
            var target_container = outerContainer;
            while (target_container != null)
            {
                if (_containerSystem.Insert(entity, target_container))
                    break;

                _containerSystem.TryGetContainingContainer((target_container.Owner, null, null), out target_container);
            }

            // If we couldn't or there was no container to insert into just dump them to the map/grid.
            if (target_container == null)
                _xformSystem.AttachToGridOrMap(entity);
        }
    }

    #endregion Consume Entities

    #region Consume Tiles

    /// <summary>
    /// Makes an event horizon consume a specific tile on a grid.
    /// </summary>
    public void ConsumeTile(EntityUid hungry, TileRef tile, EventHorizonComponent eventHorizon)
    {
        ConsumeTiles(hungry, new List<(Vector2i, Tile)>(new[] { (tile.GridIndices, Tile.Empty) }), tile.GridUid, Comp<MapGridComponent>(tile.GridUid), eventHorizon);
    }

    /// <summary>
    /// Makes an event horizon attempt to consume a specific tile on a grid.
    /// </summary>
    public void AttemptConsumeTile(EntityUid hungry, TileRef tile, EventHorizonComponent eventHorizon)
    {
        AttemptConsumeTiles(hungry, new TileRef[1] { tile }, tile.GridUid, Comp<MapGridComponent>(tile.GridUid), eventHorizon);
    }

    /// <summary>
    /// Makes an event horizon consume a set of tiles on a grid.
    /// </summary>
    public void ConsumeTiles(EntityUid hungry, List<(Vector2i, Tile)> tiles, EntityUid gridId, MapGridComponent grid, EventHorizonComponent eventHorizon)
    {
        if (tiles.Count <= 0)
            return;

        var ev = new TilesConsumedByEventHorizonEvent(tiles, gridId, grid, hungry, eventHorizon);
        RaiseLocalEvent(hungry, ref ev);
        _mapSystem.SetTiles(gridId, grid, tiles);
    }

    /// <summary>
    /// Makes an event horizon attempt to consume a set of tiles on a grid.
    /// </summary>
    public int AttemptConsumeTiles(EntityUid hungry, IEnumerable<TileRef> tiles, EntityUid gridId, MapGridComponent grid, EventHorizonComponent eventHorizon)
    {
        var toConsume = new List<(Vector2i, Tile)>();
        foreach (var tile in tiles)
        {
            if (CanConsumeTile(hungry, tile, grid, eventHorizon))
                toConsume.Add((tile.GridIndices, Tile.Empty));
        }

        var result = toConsume.Count;
        if (toConsume.Count > 0)
            ConsumeTiles(hungry, toConsume, gridId, grid, eventHorizon);
        return result;
    }

    /// <summary>
    /// Checks whether an event horizon can consume a given tile.
    /// This is only possible if it can also consume all entities anchored to the tile.
    /// </summary>
    public bool CanConsumeTile(EntityUid hungry, TileRef tile, MapGridComponent grid, EventHorizonComponent eventHorizon)
    {
        foreach (var blockingEntity in grid.GetAnchoredEntities(tile.GridIndices))
        {
            if (!CanConsumeEntity(hungry, blockingEntity, eventHorizon))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Consumes all tiles within a given distance of an entity.
    /// Some entities are immune to consumption.
    /// </summary>
    public void ConsumeTilesInRange(EntityUid uid, float range, TransformComponent? xform, EventHorizonComponent? eventHorizon)
    {
        if (!Resolve(uid, ref xform) || !Resolve(uid, ref eventHorizon))
            return;

        var mapPos = _xformSystem.GetMapCoordinates(uid, xform: xform);
        var box = Box2.CenteredAround(mapPos.Position, new Vector2(range, range));
        var circle = new Circle(mapPos.Position, range);
        var grids = new List<Entity<MapGridComponent>>();
        _mapMan.FindGridsIntersecting(mapPos.MapId, box, ref grids);

        foreach (var grid in grids)
        {
            // TODO: Remover grid.Owner when this iterator returns entityuids as well.
            AttemptConsumeTiles(uid, _mapSystem.GetTilesIntersecting(grid.Owner, grid.Comp, circle), grid, grid, eventHorizon);
        }
    }

    #endregion Consume Tiles

    /// <summary>
    /// Consumes most entities and tiles within a given distance of an entity.
    /// Some entities are immune to consumption.
    /// </summary>
    public void ConsumeEverythingInRange(EntityUid uid, float range, TransformComponent? xform = null, EventHorizonComponent? eventHorizon = null)
    {
        if (!Resolve(uid, ref eventHorizon))
            return;

        if (eventHorizon.ConsumeEntities)
            ConsumeEntitiesInRange(uid, range, null, eventHorizon);
        if (eventHorizon.ConsumeTiles)
            ConsumeTilesInRange(uid, range, xform, eventHorizon);
    }

    #endregion Consume

    #region Getters/Setters

    /// <summary>
    /// Sets how often an event horizon will scan for overlapping entities to consume.
    /// The value is specifically how long the subsystem should wait between scans.
    /// If the new scanning period would have already prompted a scan given the previous scan time one is prompted immediately.
    /// </summary>
    public void SetConsumePeriod(EntityUid uid, TimeSpan value, EventHorizonComponent? eventHorizon = null)
    {
        if (!Resolve(uid, ref eventHorizon))
            return;

        if (MathHelper.CloseTo(eventHorizon.TargetConsumePeriod.TotalSeconds, value.TotalSeconds))
            return;

        var diff = (value - eventHorizon.TargetConsumePeriod);
        eventHorizon.TargetConsumePeriod = value;
        eventHorizon.NextConsumeWaveTime += diff;

        var curTime = _timing.CurTime;
        if (eventHorizon.NextConsumeWaveTime < curTime)
            Update(uid, eventHorizon);
    }

    #endregion Getters/Setters

    #region Event Handlers

    /// <summary>
    /// Prevents a singularity from colliding with anything it is incapable of consuming.
    /// </summary>
    protected override bool PreventCollide(EntityUid uid, EventHorizonComponent comp, ref PreventCollideEvent args)
    {
        if (base.PreventCollide(uid, comp, ref args) || args.Cancelled)
            return true;

        // If we can eat it we don't want to bounce off of it. If we can't eat it we want to bounce off of it (containment fields).
        args.Cancelled = args.OurFixture.Hard && CanConsumeEntity(uid, args.OtherEntity, comp);
        return false;
    }

    /// <summary>
    /// A generic event handler that prevents singularities from consuming entities with a component of a given type if registered.
    /// </summary>
    public static void PreventConsume<TComp>(EntityUid uid, TComp comp, ref EventHorizonAttemptConsumeEntityEvent args)
    {
        if (!args.Cancelled)
            args.Cancelled = true;
    }

    /// <summary>
    /// A generic event handler that prevents singularities from breaching containment.
    /// In this case 'breaching containment' means consuming an entity with a component of the given type unless the event horizon is set to breach containment anyway.
    /// </summary>
    public static void PreventBreach<TComp>(EntityUid uid, TComp comp, ref EventHorizonAttemptConsumeEntityEvent args)
    {
        if (args.Cancelled)
            return;
        if (!args.EventHorizon.CanBreachContainment)
            PreventConsume(uid, comp, ref args);
    }

    /// <summary>
    /// Handles event horizons consuming any entities they bump into.
    /// The event horizon will not consume any entities if it itself has been consumed by an event horizon.
    /// </summary>
    private void OnStartCollide(EntityUid uid, EventHorizonComponent comp, ref StartCollideEvent args)
    {
        if (comp.BeingConsumedByAnotherEventHorizon)
            return;
        if (args.OurFixtureId != comp.ConsumerFixtureId)
            return;

        AttemptConsumeEntity(uid, args.OtherEntity, comp);
    }

    /// <summary>
    /// Prevents two event horizons from annihilating one another.
    /// Specifically prevents event horizons from consuming themselves.
    /// Also ensures that if this event horizon has already been consumed by another event horizon it cannot be consumed again.
    /// </summary>
    private void OnAnotherEventHorizonAttemptConsumeThisEventHorizon(EntityUid uid, EventHorizonComponent comp, ref EventHorizonAttemptConsumeEntityEvent args)
    {
        if (!args.Cancelled && (args.EventHorizon == comp || comp.BeingConsumedByAnotherEventHorizon))
            args.Cancelled = true;
    }

    /// <summary>
    /// Prevents two singularities from annihilating one another.
    /// Specifically ensures if this event horizon is consumed by another event horizon it knows that it has been consumed.
    /// </summary>
    private void OnAnotherEventHorizonConsumedThisEventHorizon(EntityUid uid, EventHorizonComponent comp, ref EventHorizonConsumedEntityEvent args)
    {
        comp.BeingConsumedByAnotherEventHorizon = true;
    }

    /// <summary>
    /// Handles event horizons deciding to escape containers they are inserted into.
    /// Delegates the actual escape to <see cref="OnEventHorizonContained(EventHorizonContainedEvent)" /> on a delay.
    /// This ensures that the escape is handled after all other handlers for the insertion event and satisfies the assertion that
    ///     the inserted entity SHALL be inside of the specified container after all handles to the entity event
    ///     <see cref="EntGotInsertedIntoContainerMessage" /> are processed.
    /// </summary>
    private void OnEventHorizonContained(EntityUid uid, EventHorizonComponent comp, EntGotInsertedIntoContainerMessage args)
    {
        // Delegates processing an event until all queued events have been processed.
        QueueLocalEvent(new EventHorizonContainedEvent(uid, comp, args));
    }

    /// <summary>
    /// Handles event horizons attempting to escape containers they have been inserted into.
    /// If the event horizon has not been consumed by another event horizon this handles making the event horizon consume the containing
    ///     container and drop the the next innermost contaning container.
    /// This loops until the event horizon has escaped to the map or wound up in an indestructible container.
    /// </summary>
    private void OnEventHorizonContained(EventHorizonContainedEvent args)
    {
        var uid = args.Entity;
        if (!EntityManager.EntityExists(uid))
            return;
        var comp = args.EventHorizon;
        if (comp.BeingConsumedByAnotherEventHorizon)
            return;

        var containerEntity = args.Args.Container.Owner;
        if (!EntityManager.EntityExists(containerEntity))
            return;
        if (AttemptConsumeEntity(uid, containerEntity, comp))
            return; // If we consume the entity we also consume everything in the containers it has.

        ConsumeEntitiesInContainer(uid, args.Args.Container, comp, args.Args.Container);
    }

    /// <summary>
    /// Recursively consumes all entities within a container that is consumed by the singularity.
    /// If an entity within a consumed container cannot be consumed itself it is removed from the container.
    /// </summary>
    private void OnContainerConsumed(EntityUid uid, ContainerManagerComponent comp, ref EventHorizonConsumedEntityEvent args)
    {
        var drop_container = args.Container;
        if (drop_container is null)
            _containerSystem.TryGetContainingContainer((uid, null, null), out drop_container);

        foreach (var container in comp.GetAllContainers())
        {
            ConsumeEntitiesInContainer(args.EventHorizonUid, container, args.EventHorizon, drop_container);
        }
    }
    #endregion Event Handlers
}
