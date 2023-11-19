using System.Linq;
using System.Numerics;
using System.Reflection;
using Content.Server.Explosion.Components;
using Content.Shared.CCVar;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Explosion;
using Content.Shared.Maps;
using Content.Shared.Mind.Components;
using Content.Shared.Physics;
using Content.Shared.Projectiles;
using Robust.Shared.Spawners;
using Content.Shared.Tag;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using TimedDespawnComponent = Robust.Shared.Spawners.TimedDespawnComponent;

namespace Content.Server.Explosion.EntitySystems;

public sealed partial class ExplosionSystem
{
    /// <summary>
    ///     Used to limit explosion processing time. See <see cref="MaxProcessingTime"/>.
    /// </summary>
    internal readonly Stopwatch Stopwatch = new();

    /// <summary>
    ///     How many tiles to explode before checking the stopwatch timer
    /// </summary>
    internal static int TileCheckIteration = 1;

    /// <summary>
    ///     Queue for delayed processing of explosions. If there is an explosion that covers more than <see
    ///     cref="TilesPerTick"/> tiles, other explosions will actually be delayed slightly. Unless it's a station
    ///     nuke, this delay should never really be noticeable.
    /// </summary>
    private Queue<Func<Explosion?>> _explosionQueue = new();

    /// <summary>
    ///     The explosion currently being processed.
    /// </summary>
    private Explosion? _activeExplosion;

    /// <summary>
    ///     While processing an explosion, the "progress" is sent to clients, so that the explosion fireball effect
    ///     syncs up with the damage. When the tile iteration increments, an update needs to be sent to clients.
    ///     This integer keeps track of the last value sent to clients.
    /// </summary>
    private int _previousTileIteration;

    private List<EntityUid> _anchored = new();

    private void OnMapChanged(MapChangedEvent ev)
    {
        // If a map was deleted, check the explosion currently being processed belongs to that map.
        if (ev.Created)
            return;

        if (_activeExplosion?.Epicenter.MapId != ev.Map)
            return;

        QueueDel(_activeExplosion.VisualEnt);
        _activeExplosion = null;
        _nodeGroupSystem.PauseUpdating = false;
        _pathfindingSystem.PauseUpdating = false;
    }

    /// <summary>
    ///     Process the explosion queue.
    /// </summary>
    public override void Update(float frameTime)
    {
        if (_activeExplosion == null && _explosionQueue.Count == 0)
            // nothing to do
            return;

        Stopwatch.Restart();
        var x = Stopwatch.Elapsed.TotalMilliseconds;

        var availableTime = MaxProcessingTime;

        var tilesRemaining = TilesPerTick;
        while (tilesRemaining > 0 && MaxProcessingTime > Stopwatch.Elapsed.TotalMilliseconds)
        {
            // if there is no active explosion, get a new one to process
            if (_activeExplosion == null)
            {
                // EXPLOSION TODO allow explosion spawning to be interrupted by time limit. In the meantime, ensure that
                // there is at-least 1ms of time left before creating a new explosion
                if (MathF.Max(MaxProcessingTime - 1, 0.1f)  < Stopwatch.Elapsed.TotalMilliseconds)
                    break;

                if (!_explosionQueue.TryDequeue(out var spawnNextExplosion))
                    break;

                _activeExplosion = spawnNextExplosion();

                // explosion spawning can be null if something somewhere went wrong. (e.g., negative explosion
                // intensity).
                if (_activeExplosion == null)
                    continue;

                _previousTileIteration = 0;

                // just a lil nap
                if (SleepNodeSys)
                {
                    _nodeGroupSystem.PauseUpdating = true;
                    _pathfindingSystem.PauseUpdating = true;
                    // snooze grid-chunk regeneration?
                    // snooze power network (recipients look for new suppliers as wires get destroyed).
                }

                if (_activeExplosion.Area > SingleTickAreaLimit)
                    break; // start processing next turn.
            }

            // TODO EXPLOSION  check if active explosion is on a paused map. If it is... I guess support swapping out &
            // storing the "currently active" explosion?

#if EXCEPTION_TOLERANCE
            try
            {
#endif
                var processed = _activeExplosion.Process(tilesRemaining);
                tilesRemaining -= processed;

                // has the explosion finished processing?
                if (_activeExplosion.FinishedProcessing)
            {
                var comp = EnsureComp<TimedDespawnComponent>(_activeExplosion.VisualEnt);
                comp.Lifetime = _cfg.GetCVar(CCVars.ExplosionPersistence);
                _appearance.SetData(_activeExplosion.VisualEnt, ExplosionAppearanceData.Progress, int.MaxValue);
                _activeExplosion = null;
            }
#if EXCEPTION_TOLERANCE
            }
            catch (Exception e)
            {
                // Ensure the system does not get stuck in an error-loop.
                if (_activeExplosion != null)
                    QueueDel(_activeExplosion.VisualEnt);
                _activeExplosion = null;
                _nodeGroupSystem.PauseUpdating = false;
                _pathfindingSystem.PauseUpdating = false;
                throw;
            }
#endif
        }

        Logger.InfoS("Explosion", $"Processed {TilesPerTick - tilesRemaining} tiles in {Stopwatch.Elapsed.TotalMilliseconds}ms");

        // we have finished processing our tiles. Is there still an ongoing explosion?
        if (_activeExplosion != null)
        {
            _appearance.SetData(_activeExplosion.VisualEnt, ExplosionAppearanceData.Progress, _activeExplosion.CurrentIteration + 1);
            return;
        }

        if (_explosionQueue.Count > 0)
            return;

        //wakey wakey
        _nodeGroupSystem.PauseUpdating = false;
        _pathfindingSystem.PauseUpdating = false;
    }

    /// <summary>
    ///     Determines whether an entity is blocking a tile or not. (whether it can prevent the tile from being uprooted
    ///     by an explosion).
    /// </summary>
    /// <remarks>
    ///     Used for a variation of <see cref="TurfHelpers.IsBlockedTurf()"/> that makes use of the fact that we have
    ///     already done an entity lookup on a tile, and don't need to do so again.
    /// </remarks>
    public bool IsBlockingTurf(EntityUid uid)
    {
        if (EntityManager.IsQueuedForDeletion(uid))
            return false;

        if (!_physicsQuery.TryGetComponent(uid, out var physics))
            return false;

        return physics.CanCollide && physics.Hard && (physics.CollisionLayer & (int) CollisionGroup.Impassable) != 0;
    }

    /// <summary>
    ///     Find entities on a grid tile using the EntityLookupComponent and apply explosion effects.
    /// </summary>
    /// <returns>True if the underlying tile can be uprooted, false if the tile is blocked by a dense entity</returns>
    internal bool ExplodeTile(BroadphaseComponent lookup,
        Entity<MapGridComponent> grid,
        Vector2i tile,
        float throwForce,
        DamageSpecifier damage,
        MapCoordinates epicenter,
        HashSet<EntityUid> processed,
        string id)
    {
        var size = grid.Comp.TileSize;
        var gridBox = new Box2(tile * size, (tile + 1) * size);

        // get the entities on a tile. Note that we cannot process them directly, or we get
        // enumerator-changed-while-enumerating errors.
        List<(EntityUid, TransformComponent)> list = new();
        var state = (list, processed, _transformQuery);

        // get entities:
        lookup.DynamicTree.QueryAabb(ref state, GridQueryCallback, gridBox, true);
        lookup.StaticTree.QueryAabb(ref state, GridQueryCallback, gridBox, true);
        lookup.SundriesTree.QueryAabb(ref state, GridQueryCallback, gridBox, true);
        lookup.StaticSundriesTree.QueryAabb(ref state, GridQueryCallback, gridBox, true);

        // process those entities
        foreach (var (uid, xform) in list)
        {
            ProcessEntity(uid, epicenter, damage, throwForce, id, xform);
        }

        // process anchored entities
        var tileBlocked = false;
        _anchored.Clear();
        _map.GetAnchoredEntities(grid, tile, _anchored);
        foreach (var entity in _anchored)
        {
            processed.Add(entity);
            ProcessEntity(entity, epicenter, damage, throwForce, id, null);
        }

        // Walls and reinforced walls will break into girders. These girders will also be considered turf-blocking for
        // the purposes of destroying floors. Again, ideally the process of damaging an entity should somehow return
        // information about the entities that were spawned as a result, but without that information we just have to
        // re-check for new anchored entities. Compared to entity spawning & deleting, this should still be relatively minor.
        if (_anchored.Count > 0)
        {
            _anchored.Clear();
            _map.GetAnchoredEntities(grid, tile, _anchored);
            foreach (var entity in _anchored)
            {
                tileBlocked |= IsBlockingTurf(entity);
            }
        }

        // Next, we get the intersecting entities AGAIN, but purely for throwing. This way, glass shards spawned from
        // windows will be flung outwards, and not stay where they spawned. This is however somewhat unnecessary, and a
        // prime candidate for computational cost-cutting. Alternatively, it would be nice if there was just some sort
        // of spawned-on-destruction event that could be used to automatically assemble a list of new entities that need
        // to be thrown.
        //
        // All things considered, until entity spawning & destruction is sped up, this isn't all that time consuming.
        // And throwing is disabled for nukes anyways.
        if (throwForce <= 0)
            return !tileBlocked;

        list.Clear();
        lookup.DynamicTree.QueryAabb(ref state, GridQueryCallback, gridBox, true);
        lookup.SundriesTree.QueryAabb(ref state, GridQueryCallback, gridBox, true);

        foreach (var (uid, xform) in list)
        {
            // Here we only throw, no dealing damage. Containers n such might drop their entities after being destroyed, but
            // they should handle their own damage pass-through, with their own damage reduction calculation.
            ProcessEntity(uid, epicenter, null, throwForce, id, xform);
        }

        return !tileBlocked;
    }

    private bool GridQueryCallback(
        ref (List<(EntityUid, TransformComponent)> List, HashSet<EntityUid> Processed, EntityQuery<TransformComponent> XformQuery) state,
        in EntityUid uid)
    {
        if (state.Processed.Add(uid) && state.XformQuery.TryGetComponent(uid, out var xform))
            state.List.Add((uid, xform));

        return true;
    }

    private bool GridQueryCallback(
        ref (List<(EntityUid, TransformComponent)> List, HashSet<EntityUid> Processed, EntityQuery<TransformComponent> XformQuery) state,
        in FixtureProxy proxy)
    {
        var owner = proxy.Entity;
        return GridQueryCallback(ref state, in owner);
    }

    /// <summary>
    ///     Same as <see cref="ExplodeTile"/>, but for SPAAAAAAACE.
    /// </summary>
    internal void ExplodeSpace(BroadphaseComponent lookup,
        Matrix3 spaceMatrix,
        Matrix3 invSpaceMatrix,
        Vector2i tile,
        float throwForce,
        DamageSpecifier damage,
        MapCoordinates epicenter,
        HashSet<EntityUid> processed,
        string id)
    {
        var gridBox = Box2.FromDimensions(tile * DefaultTileSize, new Vector2(DefaultTileSize, DefaultTileSize));
        var worldBox = spaceMatrix.TransformBox(gridBox);
        var list = new List<(EntityUid, TransformComponent)>();
        var state = (list, processed, invSpaceMatrix, lookup.Owner, _transformQuery, gridBox);

        // get entities:
        lookup.DynamicTree.QueryAabb(ref state, SpaceQueryCallback, worldBox, true);
        lookup.StaticTree.QueryAabb(ref state, SpaceQueryCallback, worldBox, true);
        lookup.SundriesTree.QueryAabb(ref state, SpaceQueryCallback, worldBox, true);
        lookup.StaticSundriesTree.QueryAabb(ref state, SpaceQueryCallback, worldBox, true);

        foreach (var (uid, xform) in state.Item1)
        {
            processed.Add(uid);
            ProcessEntity(uid, epicenter, damage, throwForce, id, xform);
        }

        if (throwForce <= 0)
            return;

        // Also, throw any entities that were spawned as shrapnel. Compared to entity spawning & destruction, this extra
        // lookup is relatively minor computational cost, and throwing is disabled for nukes anyways.
        list.Clear();
        lookup.DynamicTree.QueryAabb(ref state, SpaceQueryCallback, worldBox, true);
        lookup.SundriesTree.QueryAabb(ref state, SpaceQueryCallback, worldBox, true);

        foreach (var (uid, xform) in list)
        {
            ProcessEntity(uid, epicenter, null, throwForce, id, xform);
        }
    }

    private bool SpaceQueryCallback(
        ref (List<(EntityUid, TransformComponent)> List, HashSet<EntityUid> Processed, Matrix3 InvSpaceMatrix, EntityUid LookupOwner, EntityQuery<TransformComponent> XformQuery, Box2 GridBox) state,
        in EntityUid uid)
    {
        if (state.Processed.Contains(uid))
            return true;

        var xform = state.XformQuery.GetComponent(uid);

        if (xform.ParentUid == state.LookupOwner)
        {
            // parented directly to the map, use local position
            if (state.GridBox.Contains(state.InvSpaceMatrix.Transform(xform.LocalPosition)))
                state.List.Add((uid, xform));

            return true;
        }

        // finally check if it intersects our tile
        if (state.GridBox.Contains(state.InvSpaceMatrix.Transform(_transformSystem.GetWorldPosition(xform, state.XformQuery))))
            state.List.Add((uid, xform));

        return true;
    }

    private bool SpaceQueryCallback(
        ref (List<(EntityUid, TransformComponent)> List, HashSet<EntityUid> Processed, Matrix3 InvSpaceMatrix, EntityUid LookupOwner, EntityQuery<TransformComponent> XformQuery, Box2 GridBox) state,
        in FixtureProxy proxy)
    {
        var uid = proxy.Entity;
        return SpaceQueryCallback(ref state, in uid);
    }

    /// <summary>
    ///     This function actually applies the explosion affects to an entity.
    /// </summary>
    private void ProcessEntity(
        EntityUid uid,
        MapCoordinates epicenter,
        DamageSpecifier? damage,
        float throwForce,
        string id,
        TransformComponent? xform)
    {
        // damage
        if (damage != null && _damageQuery.TryGetComponent(uid, out var damageable))
        {
            // TODO Explosion Performance
            // Cache this? I.e., instead of raising an event, check for a component?
            var ev = new GetExplosionResistanceEvent(id);
            RaiseLocalEvent(uid, ref ev);

            ev.DamageCoefficient = Math.Max(0, ev.DamageCoefficient);

            // TODO explosion entity
            // Move explosion data into the existing explosion visuals entity
            // Give each explosion a unique name, include in admin logs.

            // TODO Explosion Performance
            // This creates a new dictionary. Maybe we should just re-use a private local damage specifier and update it.
            // Though most entities shouldn't have explosion resistance, so maybe its fine.
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (ev.DamageCoefficient != 1)
                damage *= ev.DamageCoefficient;

            // Log damage to players. Damage is logged before dealing damage so that the position can be logged before
            // the entity gets deleted.
            if (_mindQuery.HasComponent(uid))
            {
                _adminLogger.Add(LogType.Explosion, LogImpact.Medium,
                    $"Explosion caused [{damage.Total}] damage to {ToPrettyString(uid):target} at {xform?.Coordinates}");
            }

            _damageableSystem.TryChangeDamage(uid, damage, ignoreResistances: true, damageable: damageable);
        }

        // if it's a container, try to damage all its contents
        if (_containersQuery.TryGetComponent(uid, out var containers))
        {
            foreach (var container in containers.Containers.Values)
            {
                foreach (var ent in container.ContainedEntities)
                {
                    // setting throw force to 0 to prevent offset items inside containers
                    ProcessEntity(ent, epicenter, damage, 0f, id, _transformQuery.GetComponent(uid));
                }
            }
        }

        // throw
        if (xform != null // null implies anchored
            && !xform.Anchored
            && throwForce > 0
            && !EntityManager.IsQueuedForDeletion(uid)
            && _physicsQuery.TryGetComponent(uid, out var physics)
            && physics.BodyType == BodyType.Dynamic)
        {
            var pos = _transformSystem.GetWorldPosition(xform);
            _throwingSystem.TryThrow(
                uid,
                 pos - epicenter.Position,
                physics,
                xform,
                _projectileQuery,
                throwForce);
        }

        // TODO EXPLOSION puddle / flammable ignite?

        // TODO EXPLOSION deaf/ear damage? other explosion effects?
    }

    /// <summary>
    ///     Tries to damage floor tiles. Not to be confused with the function that damages entities intersecting the
    ///     grid tile.
    /// </summary>
    public void DamageFloorTile(TileRef tileRef,
        float effectiveIntensity,
        int maxTileBreak,
        bool canCreateVacuum,
        List<(Vector2i GridIndices, Tile Tile)> damagedTiles,
        ExplosionPrototype type)
    {
        if (_tileDefinitionManager[tileRef.Tile.TypeId] is not ContentTileDefinition tileDef)
            return;

        if (tileDef.IsSpace)
            canCreateVacuum = true; // is already a vacuum.

        int tileBreakages = 0;
        while (maxTileBreak > tileBreakages && _robustRandom.Prob(type.TileBreakChance(effectiveIntensity)))
        {
            tileBreakages++;
            effectiveIntensity -= type.TileBreakRerollReduction;

            // does this have a base-turf that we can break it down to?
            if (string.IsNullOrEmpty(tileDef.BaseTurf))
                break;

            if (_tileDefinitionManager[tileDef.BaseTurf] is not ContentTileDefinition newDef)
                break;

            if (newDef.IsSpace && !canCreateVacuum)
                break;

            tileDef = newDef;
        }

        if (tileDef.TileId == tileRef.Tile.TypeId)
            return;

        damagedTiles.Add((tileRef.GridIndices, new Tile(tileDef.TileId)));
    }
}

/// <summary>
///     This is a data class that stores information about the area affected by an explosion, for processing by <see
///     cref="ExplosionSystem"/>.
/// </summary>
/// <remarks>
///     This is basically the output of <see cref="ExplosionSystem.GetExplosionTiles()"/>, but with some utility functions for
///     iterating over the tiles, along with the ability to keep track of what entities have already been damaged by
///     this explosion.
/// </remarks>
sealed class Explosion
{
    /// <summary>
    ///     For every grid (+ space) that the explosion reached, this data struct stores information about the tiles and
    ///     caches the entity-lookup component so that it doesn't have to be re-fetched for every tile.
    /// </summary>
    struct ExplosionData
    {
        /// <summary>
        ///     The tiles that the explosion damaged, grouped by the iteration (can be thought of as the distance from the epicenter)
        /// </summary>
        public Dictionary<int, List<Vector2i>> TileLists;

        /// <summary>
        ///     Lookup component for this grid (or space/map).
        /// </summary>
        public BroadphaseComponent Lookup;

        /// <summary>
        ///     The actual grid that this corresponds to. If null, this implies space.
        /// </summary>
        public MapGridComponent? MapGrid;
    }

    private readonly List<ExplosionData> _explosionData = new();

    /// <summary>
    ///     The explosion intensity associated with each tile iteration.
    /// </summary>
    private readonly List<float> _tileSetIntensity;

    /// <summary>
    ///     Used to avoid applying explosion effects repeatedly to the same entity. Particularly important if the
    ///     explosion throws this entity, as then it will be moving while the explosion is happening.
    /// </summary>
    public readonly HashSet<EntityUid> ProcessedEntities = new();

    /// <summary>
    ///     This integer tracks how much of this explosion has been processed.
    /// </summary>
    public int CurrentIteration { get; private set; } = 0;

    /// <summary>
    ///     The prototype for this explosion. Determines tile break chance, damage, etc.
    /// </summary>
    public readonly ExplosionPrototype ExplosionType;

    /// <summary>
    ///     The center of the explosion. Used for physics throwing. Also used to identify the map on which the explosion is happening.
    /// </summary>
    public readonly MapCoordinates Epicenter;

    /// <summary>
    ///     The matrix that defines the reference frame for the explosion in space.
    /// </summary>
    private readonly Matrix3 _spaceMatrix;

    /// <summary>
    ///     Inverse of <see cref="_spaceMatrix"/>
    /// </summary>
    private readonly Matrix3 _invSpaceMatrix;

    /// <summary>
    ///     Have all the tiles on all the grids been processed?
    /// </summary>
    public bool FinishedProcessing;

    // Variables used for enumerating over tiles, grids, etc
    private DamageSpecifier _currentDamage = default!;
#if DEBUG
    private DamageSpecifier? _expectedDamage;
#endif
    private BroadphaseComponent _currentLookup = default!;
    private MapGridComponent? _currentGrid;
    private float _currentIntensity;
    private float _currentThrowForce;
    private List<Vector2i>.Enumerator _currentEnumerator;
    private int _currentDataIndex;

    /// <summary>
    ///     The set of tiles that need to be updated when the explosion has finished processing. Used to avoid having
    ///     the explosion trigger chunk regeneration & shuttle-system processing every tick.
    /// </summary>
    private readonly Dictionary<MapGridComponent, List<(Vector2i, Tile)>> _tileUpdateDict = new();

    // Entity Queries
    private readonly EntityQuery<TransformComponent> _xformQuery;
    private readonly EntityQuery<PhysicsComponent> _physicsQuery;
    private readonly EntityQuery<DamageableComponent> _damageQuery;
    private readonly EntityQuery<ProjectileComponent> _projectileQuery;
    private readonly EntityQuery<TagComponent> _tagQuery;

    /// <summary>
    ///     Total area that the explosion covers.
    /// </summary>
    public readonly int Area;

    /// <summary>
    ///     factor used to scale the tile break chances.
    /// </summary>
    private readonly float _tileBreakScale;

    /// <summary>
    ///     Maximum number of times that an explosion will break a single tile.
    /// </summary>
    private readonly int _maxTileBreak;

    /// <summary>
    ///     Whether this explosion can turn non-vacuum tiles into vacuum-tiles.
    /// </summary>
    private readonly bool _canCreateVacuum;

    private readonly IEntityManager _entMan;
    private readonly ExplosionSystem _system;

    public readonly EntityUid VisualEnt;

    /// <summary>
    ///     Initialize a new instance for processing
    /// </summary>
    public Explosion(ExplosionSystem system,
        ExplosionPrototype explosionType,
        ExplosionSpaceTileFlood? spaceData,
        List<ExplosionGridTileFlood> gridData,
        List<float> tileSetIntensity,
        MapCoordinates epicenter,
        Matrix3 spaceMatrix,
        int area,
        float tileBreakScale,
        int maxTileBreak,
        bool canCreateVacuum,
        IEntityManager entMan,
        IMapManager mapMan,
        EntityUid visualEnt)
    {
        VisualEnt = visualEnt;
        _system = system;
        ExplosionType = explosionType;
        _tileSetIntensity = tileSetIntensity;
        Epicenter = epicenter;
        Area = area;

        _tileBreakScale = tileBreakScale;
        _maxTileBreak = maxTileBreak;
        _canCreateVacuum = canCreateVacuum;
        _entMan = entMan;

        _xformQuery = entMan.GetEntityQuery<TransformComponent>();
        _physicsQuery = entMan.GetEntityQuery<PhysicsComponent>();
        _damageQuery = entMan.GetEntityQuery<DamageableComponent>();
        _tagQuery = entMan.GetEntityQuery<TagComponent>();
        _projectileQuery = entMan.GetEntityQuery<ProjectileComponent>();

        if (spaceData != null)
        {
            var mapUid = mapMan.GetMapEntityId(epicenter.MapId);

            _explosionData.Add(new()
            {
                TileLists = spaceData.TileLists,
                Lookup = entMan.GetComponent<BroadphaseComponent>(mapUid),
                MapGrid = null
            });

            _spaceMatrix = spaceMatrix;
            _invSpaceMatrix = Matrix3.Invert(spaceMatrix);
        }

        foreach (var grid in gridData)
        {
            _explosionData.Add(new ExplosionData
            {
                TileLists = grid.TileLists,
                Lookup = entMan.GetComponent<BroadphaseComponent>(grid.Grid.Owner),
                MapGrid = grid.Grid,
            });
        }

        if (TryGetNextTileEnumerator())
            MoveNext();
    }

    /// <summary>
    ///     Find the next tile-enumerator. This either means retrieving a set of tiles on the next grid, or incrementing
    ///     the tile iteration by one and moving back to the first grid. This will also update the current damage, current entity-lookup, etc.
    /// </summary>
    private bool TryGetNextTileEnumerator()
    {
        while (CurrentIteration < _tileSetIntensity.Count)
        {
            _currentIntensity = _tileSetIntensity[CurrentIteration];

            #if DEBUG
            if (_expectedDamage != null)
            {
                // Check that explosion processing hasn't somehow accidentally mutated the damage set.
                DebugTools.Assert(_expectedDamage.Equals(_currentDamage));
                _expectedDamage = ExplosionType.DamagePerIntensity * _currentIntensity;
            }
            #endif

            _currentDamage = ExplosionType.DamagePerIntensity * _currentIntensity;

            // only throw if either the explosion is small, or if this is the outer ring of a large explosion.
            var doThrow = Area < _system.ThrowLimit || CurrentIteration > _tileSetIntensity.Count - 6;
            _currentThrowForce = doThrow ? 10 * MathF.Sqrt(_currentIntensity) : 0;

            // for each grid/space tile set
            while (_currentDataIndex < _explosionData.Count)
            {
                // try get any tile hash-set corresponding to this intensity
                var tileSets = _explosionData[_currentDataIndex].TileLists;
                if (!tileSets.TryGetValue(CurrentIteration, out var tileList))
                {
                    _currentDataIndex++;
                    continue;
                }

                _currentEnumerator = tileList.GetEnumerator();
                _currentLookup = _explosionData[_currentDataIndex].Lookup;
                _currentGrid = _explosionData[_currentDataIndex].MapGrid;
                _currentDataIndex++;

                // sanity checks, in case something changed while the explosion was being processed over several ticks.
                if (_currentLookup.Deleted || _currentGrid != null && !_entMan.EntityExists(_currentGrid.Owner))
                    continue;

                return true;
            }

            // All the tiles belonging to this explosion iteration have been processed. Move onto the next iteration and
            // reset the grid counter.
            CurrentIteration++;
            _currentDataIndex = 0;
        }

        // No more explosion tiles to process
        FinishedProcessing = true;
        return false;
    }

    /// <summary>
    ///     Get the next tile that needs processing
    /// </summary>
    private bool MoveNext()
    {
        if (FinishedProcessing)
            return false;

        while (!FinishedProcessing)
        {
            if (_currentEnumerator.MoveNext())
                return true;
            else
                TryGetNextTileEnumerator();
        }

        return false;
    }

    /// <summary>
    ///     Attempt to process (i.e., damage entities) some number of grid tiles.
    /// </summary>
    public int Process(int processingTarget)
    {
        // In case the explosion terminated early last tick due to exceeding the allocated processing time, use this
        // time to update the tiles.
        SetTiles();

        int processed;
        for (processed = 0; processed < processingTarget; processed++)
        {
            if (processed % ExplosionSystem.TileCheckIteration == 0 &&
                _system.Stopwatch.Elapsed.TotalMilliseconds > _system.MaxProcessingTime)
            {
                break;
            }

            // Is the current tile on a grid (instead of in space)?
            if (_currentGrid != null &&
                _currentGrid.TryGetTileRef(_currentEnumerator.Current, out var tileRef) &&
                !tileRef.Tile.IsEmpty)
            {
                if (!_tileUpdateDict.TryGetValue(_currentGrid, out var tileUpdateList))
                {
                    tileUpdateList = new();
                    _tileUpdateDict[_currentGrid] = tileUpdateList;
                }

                // damage entities on the tile. Also figures out whether there are any solid entities blocking the floor
                // from being destroyed.
                var canDamageFloor = _system.ExplodeTile(_currentLookup,
                    (_currentGrid.Owner, _currentGrid),
                    _currentEnumerator.Current,
                    _currentThrowForce,
                    _currentDamage,
                    Epicenter,
                    ProcessedEntities,
                    ExplosionType.ID);

                // If the floor is not blocked by some dense object, damage the floor tiles.
                if (canDamageFloor)
                    _system.DamageFloorTile(tileRef, _currentIntensity * _tileBreakScale, _maxTileBreak, _canCreateVacuum, tileUpdateList, ExplosionType);
            }
            else
            {
                // The current "tile" is in space. Damage any entities in that region
                _system.ExplodeSpace(_currentLookup,
                    _spaceMatrix,
                    _invSpaceMatrix,
                    _currentEnumerator.Current,
                    _currentThrowForce,
                    _currentDamage,
                    Epicenter,
                    ProcessedEntities,
                    ExplosionType.ID);
            }

            if (!MoveNext())
                break;
        }

        // Update damaged/broken tiles on the grid.
        SetTiles();
        return processed;
    }

    private void SetTiles()
    {
        // Updating the grid can result in chunk collision regeneration & slow processing by the shuttle system.
        // Therefore, tile breaking may be configure to only happen at the end of an explosion, rather than during every
        // tick.
        if (!_system.IncrementalTileBreaking && !FinishedProcessing)
            return;

        foreach (var (grid, list) in _tileUpdateDict)
        {
            if (list.Count > 0 && _entMan.EntityExists(grid.Owner))
            {
                grid.SetTiles(list);
            }
        }
        _tileUpdateDict.Clear();
    }
}

