using System.Numerics;
using Content.Server.Administration.Logs;
using Content.Server.Singularity.Events;
using Content.Shared.Database;
using Content.Shared.Mind.Components;
using Content.Shared.Singularity.Components;
using Content.Shared.Singularity.EntitySystems;
using Content.Shared.Station.Components;
using Content.Shared.Tag;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
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

    private static readonly ProtoId<TagPrototype> HighRiskItemTag = "HighRiskItem";

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
        vvHandle.AddPath(nameof(EventHorizonComponent.TargetConsumePeriod), (_, comp) => comp.TargetConsumePeriod, (uid, value, comp) => SetConsumePeriod((uid, comp), value));
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
                Update((uid, eventHorizon, xform));
        }
    }

    /// <summary>
    /// Makes an event horizon consume everything nearby and resets the cooldown it for the next automated wave.
    /// </summary>
    public void Update(Entity<EventHorizonComponent?, TransformComponent?> entity)
    {
        var (uid, eventHorizon, xform) = entity;

        if (!HorizonQuery.Resolve(uid, ref eventHorizon))
            return;

        eventHorizon.NextConsumeWaveTime += eventHorizon.TargetConsumePeriod;
        if (eventHorizon.BeingConsumedByAnotherEventHorizon)
            return;

        if (!Resolve(uid, ref xform))
            return;

        // Handle singularities some admin smited into a locker.
        if (_containerSystem.TryGetContainingContainer((uid, xform, null), out var container)
            && !AttemptConsumeEntity((uid, eventHorizon), container.Owner))
        {
            // Locker is indestructible. Consume everything else in the locker instead of magically teleporting out.
            ConsumeEntitiesInContainer((uid, eventHorizon), container);
            return;
        }

        if (eventHorizon.Radius > 0.0f)
            ConsumeEverythingInRange((uid, eventHorizon, xform), eventHorizon.Radius);
    }

    #region Consume

    #region Consume Entities

    /// <summary>
    /// Makes an event horizon consume a given entity.
    /// </summary>
    public void ConsumeEntity(Entity<EventHorizonComponent> hungry, EntityUid morsel, BaseContainer? outerContainer = null)
    {
        if (EntityManager.IsQueuedForDeletion(morsel)) // already handled, and we're substepping
            return;

        if (HasComp<MindContainerComponent>(morsel)
            || _tagSystem.HasTag(morsel, HighRiskItemTag)
            || HasComp<ContainmentFieldGeneratorComponent>(morsel))
        {
            _adminLogger.Add(LogType.EntityDelete, LogImpact.High, $"{ToPrettyString(morsel):player} entered the event horizon of {ToPrettyString(hungry)} and was deleted");
        }

        QueueDel(morsel);

        var evSelf = new EntityConsumedByEventHorizonEvent(hungry, morsel, outerContainer);
        var evEaten = new EventHorizonConsumedEntityEvent(hungry, morsel, outerContainer);
        RaiseLocalEvent(hungry, ref evSelf);
        RaiseLocalEvent(morsel, ref evEaten);
    }

    /// <summary>
    /// Makes an event horizon attempt to consume a given entity.
    /// </summary>
    public bool AttemptConsumeEntity(Entity<EventHorizonComponent> hungry, EntityUid morsel, BaseContainer? outerContainer = null)
    {
        if (!CanConsumeEntity(hungry, morsel))
            return false;

        ConsumeEntity(hungry, morsel, outerContainer);
        return true;
    }

    /// <summary>
    /// Checks whether an event horizon can consume a given entity.
    /// </summary>
    public bool CanConsumeEntity(Entity<EventHorizonComponent> hungry, EntityUid uid)
    {
        var ev = new EventHorizonAttemptConsumeEntityEvent(hungry, uid);
        RaiseLocalEvent(uid, ref ev);
        return !ev.Cancelled;
    }

    /// <summary>
    /// Attempts to consume all entities within a given distance of an entity;
    /// Excludes the center entity.
    /// </summary>
    public void ConsumeEntitiesInRange(Entity<EventHorizonComponent?, PhysicsComponent?> horizon, float range)
    {
        var (uid, eventHorizon, body) = horizon;

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

            AttemptConsumeEntity((uid, eventHorizon), entity);
        }
    }

    /// <summary>
    /// Attempts to consume all entities within a container.
    /// Excludes the event horizon itself.
    /// All immune entities within the container will be dumped to a given container or the map/grid if that is impossible.
    /// </summary>
    public void ConsumeEntitiesInContainer(Entity<EventHorizonComponent> hungry, BaseContainer container, BaseContainer? outerContainer = null)
    {
        // Removing the immune entities from the container needs to be deferred until after iteration or the iterator raises an error.
        List<EntityUid> immune = new();

        foreach (var entity in container.ContainedEntities)
        {
            if (entity == hungry.Owner || !AttemptConsumeEntity(hungry, entity, outerContainer))
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
    public void ConsumeTile(Entity<EventHorizonComponent> eventHorizon, TileRef tile)
    {
        ConsumeTiles(eventHorizon, (tile.GridUid, Comp<MapGridComponent>(tile.GridUid)), [(tile.GridIndices, Tile.Empty)]);
    }

    /// <summary>
    /// Makes an event horizon attempt to consume a specific tile on a grid.
    /// </summary>
    public void AttemptConsumeTile(Entity<EventHorizonComponent> eventHorizon, TileRef tile)
    {
        AttemptConsumeTiles(eventHorizon, (tile.GridUid, Comp<MapGridComponent>(tile.GridUid)), [tile]);
    }

    /// <summary>
    /// Makes an event horizon consume a set of tiles on a grid.
    /// </summary>
    public void ConsumeTiles(Entity<EventHorizonComponent> hungry, Entity<MapGridComponent> grid, List<(Vector2i, Tile)> tiles)
    {
        if (tiles.Count <= 0)
            return;

        var ev = new TilesConsumedByEventHorizonEvent(hungry, grid, tiles);
        RaiseLocalEvent(hungry, ref ev);
        _mapSystem.SetTiles(grid, tiles);
    }

    /// <summary>
    /// Makes an event horizon attempt to consume a set of tiles on a grid.
    /// </summary>
    public int AttemptConsumeTiles(Entity<EventHorizonComponent> hungry, Entity<MapGridComponent> grid, IEnumerable<TileRef> tiles)
    {
        var toConsume = new List<(Vector2i, Tile)>();
        foreach (var tile in tiles)
        {
            if (CanConsumeTile(hungry, tile, grid))
                toConsume.Add((tile.GridIndices, Tile.Empty));
        }

        var result = toConsume.Count;
        if (toConsume.Count > 0)
            ConsumeTiles(hungry, grid, toConsume);

        return result;
    }

    /// <summary>
    /// Checks whether an event horizon can consume a given tile.
    /// This is only possible if it can also consume all entities anchored to the tile.
    /// </summary>
    public bool CanConsumeTile(Entity<EventHorizonComponent> hungry, TileRef tile, Entity<MapGridComponent> grid)
    {
        foreach (var blockingEntity in _mapSystem.GetAnchoredEntities(grid, tile.GridIndices))
        {
            if (!CanConsumeEntity(hungry, blockingEntity))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Consumes all tiles within a given distance of an entity.
    /// Some entities are immune to consumption.
    /// </summary>
    public void ConsumeTilesInRange(Entity<EventHorizonComponent?, TransformComponent?> eventHorizon, float range)
    {
        if (!Resolve(eventHorizon, ref eventHorizon.Comp1, ref eventHorizon.Comp2))
            return;

        var mapPos = _xformSystem.GetMapCoordinates((eventHorizon.Owner, eventHorizon.Comp2));
        var box = Box2.CenteredAround(mapPos.Position, new Vector2(range, range));
        var circle = new Circle(mapPos.Position, range);
        var grids = new List<Entity<MapGridComponent>>();
        _mapMan.FindGridsIntersecting(mapPos.MapId, box, ref grids);

        foreach (var grid in grids)
        {
            AttemptConsumeTiles((eventHorizon.Owner, eventHorizon.Comp1), grid, _mapSystem.GetTilesIntersecting(grid.Owner, grid.Comp, circle));
        }
    }

    #endregion Consume Tiles

    /// <summary>
    /// Consumes most entities and tiles within a given distance of an entity.
    /// Some entities are immune to consumption.
    /// </summary>
    public void ConsumeEverythingInRange(Entity<EventHorizonComponent?, TransformComponent?> eventHorizon, float range)
    {
        if (!HorizonQuery.Resolve(eventHorizon, ref eventHorizon.Comp1))
            return;

        if (eventHorizon.Comp1.ConsumeEntities)
            ConsumeEntitiesInRange((eventHorizon, eventHorizon, null), range);

        if (eventHorizon.Comp1.ConsumeTiles)
            ConsumeTilesInRange(eventHorizon, range);
    }

    #endregion Consume

    #region Getters/Setters

    /// <summary>
    /// Sets how often an event horizon will scan for overlapping entities to consume.
    /// The value is specifically how long the subsystem should wait between scans.
    /// If the new scanning period would have already prompted a scan given the previous scan time one is prompted immediately.
    /// </summary>
    public void SetConsumePeriod(Entity<EventHorizonComponent?> horizon, TimeSpan value)
    {
        var (uid, eventHorizon) = horizon;

        if (!HorizonQuery.Resolve(uid, ref eventHorizon))
            return;

        if (MathHelper.CloseTo(eventHorizon.TargetConsumePeriod.TotalSeconds, value.TotalSeconds))
            return;

        var diff = value - eventHorizon.TargetConsumePeriod;
        eventHorizon.TargetConsumePeriod = value;
        eventHorizon.NextConsumeWaveTime += diff;

        var curTime = _timing.CurTime;
        if (eventHorizon.NextConsumeWaveTime < curTime)
            Update((uid, eventHorizon, null));
    }

    #endregion Getters/Setters

    #region Event Handlers

    /// <summary>
    /// Initializes the event horizon scan time.
    /// </summary>
    private void OnHorizonMapInit(Entity<EventHorizonComponent> eventHorizon, ref MapInitEvent args)
    {
        eventHorizon.Comp.NextConsumeWaveTime = _timing.CurTime;
    }

    /// <summary>
    /// Prevents a singularity from colliding with anything it is incapable of consuming.
    /// </summary>
    protected override bool PreventCollide(Entity<EventHorizonComponent> eventHorizon, ref PreventCollideEvent args)
    {
        if (base.PreventCollide(eventHorizon, ref args) || args.Cancelled)
            return true;

        // If we can eat it we don't want to bounce off of it. If we can't eat it we want to bounce off of it (containment fields).
        args.Cancelled = args.OurFixture.Hard && CanConsumeEntity(eventHorizon, args.OtherEntity);
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
        if (!args.EventHorizon.Comp.CanBreachContainment)
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

        AttemptConsumeEntity((uid, comp), args.OtherEntity);
    }

    /// <summary>
    /// Prevents two event horizons from annihilating one another.
    /// Specifically prevents event horizons from consuming themselves.
    /// Also ensures that if this event horizon has already been consumed by another event horizon it cannot be consumed again.
    /// </summary>
    private void OnAnotherEventHorizonAttemptConsumeThisEventHorizon(Entity<EventHorizonComponent> eventHorizon, ref EventHorizonAttemptConsumeEntityEvent args)
    {
        if (!args.Cancelled && (args.EventHorizon == eventHorizon || eventHorizon.Comp.BeingConsumedByAnotherEventHorizon))
            args.Cancelled = true;
    }

    /// <summary>
    /// Prevents two singularities from annihilating one another.
    /// Specifically ensures if this event horizon is consumed by another event horizon it knows that it has been consumed.
    /// </summary>
    private void OnAnotherEventHorizonConsumedThisEventHorizon(Entity<EventHorizonComponent> eventHorizon, ref EventHorizonConsumedEntityEvent args)
    {
        eventHorizon.Comp.BeingConsumedByAnotherEventHorizon = true;
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
        if (!Exists(uid))
            return;

        var comp = args.EventHorizon;
        if (comp.BeingConsumedByAnotherEventHorizon)
            return;

        var containerEntity = args.Args.Container.Owner;
        if (!Exists(containerEntity))
            return;

        if (AttemptConsumeEntity((uid, comp), containerEntity))
            return; // If we consume the entity we also consume everything in the containers it has.

        ConsumeEntitiesInContainer((uid, comp), args.Args.Container, args.Args.Container);
    }

    /// <summary>
    /// Recursively consumes all entities within a container that is consumed by the singularity.
    /// If an entity within a consumed container cannot be consumed itself it is removed from the container.
    /// </summary>
    private void OnContainerConsumed(Entity<ContainerManagerComponent> containerEntity, ref EventHorizonConsumedEntityEvent args)
    {
        var drop_container = args.OuterContainer;
        if (drop_container is null)
            _containerSystem.TryGetContainingContainer((containerEntity, null, null), out drop_container);

        foreach (var container in _containerSystem.GetAllContainers(containerEntity))
        {
            ConsumeEntitiesInContainer(args.EventHorizon, container, drop_container);
        }
    }

    #endregion Event Handlers

    #region Obsolete API

    /// <inheritdoc cref="Update(Entity{EventHorizonComponent?, TransformComponent?})"/>
    [Obsolete("This method is obsolete, use the Entity<T> override")]
    public void Update(EntityUid uid, EventHorizonComponent? eventHorizon = null, TransformComponent? xform = null)
    {
        Update((uid, eventHorizon, xform));
    }

    /// <inheritdoc cref="ConsumeEntity(Entity{EventHorizonComponent?}, EntityUid, BaseContainer?)"/>
    [Obsolete("This method is obsolete, use the Entity<T> override.")]
    public void ConsumeEntity(EntityUid hungry, EntityUid morsel, EventHorizonComponent eventHorizon, BaseContainer? outerContainer = null)
    {
        ConsumeEntity((hungry, eventHorizon), morsel, outerContainer);
    }

    /// <inheritdoc cref="AttemptConsumeEntity(Entity{EventHorizonComponent?}, EntityUid, BaseContainer?)"/>
    [Obsolete("This method is obsolete, use the Entity<T> override.")]
    public bool AttemptConsumeEntity(EntityUid hungry, EntityUid morsel, EventHorizonComponent eventHorizon, BaseContainer? outerContainer = null)
    {
        return AttemptConsumeEntity((hungry, eventHorizon), morsel, outerContainer);
    }

    /// <inheritdoc cref="CanConsumeEntity(Entity{EventHorizonComponent?}, EntityUid)"/>
    [Obsolete("This method is obsolete, use the Entity<T> override.")]
    public bool CanConsumeEntity(EntityUid hungry, EntityUid uid, EventHorizonComponent eventHorizon)
    {
        return CanConsumeEntity((hungry, eventHorizon), uid);
    }

    /// <inheritdoc cref="ConsumeEntitiesInRange(Entity{EventHorizonComponent?, PhysicsComponent?}, float)"/>
    [Obsolete("This method is obsolete, use the Entity<T> override.")]
    public void ConsumeEntitiesInRange(EntityUid uid, float range, PhysicsComponent? body = null, EventHorizonComponent? eventHorizon = null)
    {
        ConsumeEntitiesInRange((uid, eventHorizon, body), range);
    }

    /// <inheritdoc cref="ConsumeEntitiesInContainer(Entity{EventHorizonComponent?}, BaseContainer, BaseContainer)"/>
    [Obsolete("This method is obsolete, use the Entity<T> override.")]
    public void ConsumeEntitiesInContainer(EntityUid hungry, BaseContainer container, EventHorizonComponent eventHorizon, BaseContainer? outerContainer = null)
    {
        ConsumeEntitiesInContainer((hungry, eventHorizon), container, outerContainer);
    }

    /// <inheritdoc cref="ConsumeTile(Entity{EventHorizonComponent}, TileRef)"/>
    [Obsolete("This method is obsolete, use the Entity<T> override.")]
    public void ConsumeTile(EntityUid hungry, TileRef tile, EventHorizonComponent eventHorizon)
    {
        ConsumeTile((hungry, eventHorizon), tile);
    }

    /// <inheritdoc cref="AttemptConsumeTile(Entity{EventHorizonComponent}, TileRef)"/>
    [Obsolete("This method is obsolete, use the Entity<T> override.")]
    public void AttemptConsumeTile(EntityUid hungry, TileRef tile, EventHorizonComponent eventHorizon)
    {
        AttemptConsumeTile((hungry, eventHorizon), tile);
    }

    /// <inheritdoc cref="ConsumeTiles(Entity{EventHorizonComponent}, Entity{MapGridComponent}, List{ValueTuple{Vector2i, Tile}})"/>
    [Obsolete("This method is obsolete, use the Entity<T> override.")]
    public void ConsumeTiles(EntityUid hungry, List<(Vector2i, Tile)> tiles, EntityUid gridId, MapGridComponent grid, EventHorizonComponent eventHorizon)
    {
        ConsumeTiles((hungry, eventHorizon), (gridId, grid), tiles);
    }

    /// <inheritdoc cref="AttemptConsumeTiles(Entity{EventHorizonComponent}, Entity{MapGridComponent}, IEnumerable{TileRef})"/>
    [Obsolete("This method is obsolete, use the Entity<T> override.")]
    public int AttemptConsumeTiles(EntityUid hungry, IEnumerable<TileRef> tiles, EntityUid gridId, MapGridComponent grid, EventHorizonComponent eventHorizon)
    {
        return AttemptConsumeTiles((hungry, eventHorizon), (gridId, grid), tiles);
    }

    /// <inheritdoc cref="CanConsumeTile(Entity{EventHorizonComponent}, TileRef, Entity{MapGridComponent})"/>
    [Obsolete("This method is obsolete, use the Entity<T> override.")]
    public bool CanConsumeTile(EntityUid hungry, TileRef tile, MapGridComponent grid, EventHorizonComponent eventHorizon)
    {
        return CanConsumeTile((hungry, eventHorizon), tile, (grid.Owner, grid));
    }

    /// <inheritdoc cref="ConsumeTilesInRange(Entity{EventHorizonComponent?, TransformComponent?}, float)"/>
    [Obsolete("This method is obsolete, use the Entity<T> override.")]
    public void ConsumeTilesInRange(EntityUid uid, float range, TransformComponent? xform, EventHorizonComponent? eventHorizon)
    {
        ConsumeTilesInRange((uid, eventHorizon, xform), range);
    }

    /// <inheritdoc cref="ConsumeEverythingInRange(Entity{EventHorizonComponent?, TransformComponent?}, float)"/>
    [Obsolete("This method is obsolete, use the Entity<T> override.")]
    public void ConsumeEverythingInRange(EntityUid uid, float range, TransformComponent? xform = null, EventHorizonComponent? eventHorizon = null)
    {
        ConsumeEverythingInRange((uid, eventHorizon, xform), range);
    }

    /// <inheritdoc cref="SetConsumePeriod(Entity{EventHorizonComponent?}, TimeSpan)"/>
    [Obsolete("This method is obsolete, use the Entity<T> override.")]
    public void SetConsumePeriod(EntityUid uid, TimeSpan value, EventHorizonComponent? eventHorizon = null)
    {
        SetConsumePeriod((uid, eventHorizon), value);
    }

    #endregion Obsolete API
}
