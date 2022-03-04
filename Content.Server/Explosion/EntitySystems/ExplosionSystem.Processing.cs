using System.Linq;
using Content.Server.Explosion.Components;
using Content.Server.Throwing;
using Content.Shared.Damage;
using Content.Shared.Explosion;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Explosion.EntitySystems;

public sealed partial class ExplosionSystem : EntitySystem
{
    /// <summary>
    ///     Used to identify explosions when communicating with the client. Might be needed if more than one explosion is spawned in a single tick.
    /// </summary>
    /// <remarks>
    ///     Overflowing back to 0 should cause no issue, as long as you don't have more than 256 explosions happening in a single tick.
    /// </remarks>
    private byte _explosionCounter = 0;
    // maybe should just use a UID/explosion-entity and a state to convey information?
    // but then need to ignore PVS? Eeehh this works well enough for now.

    /// <summary>
    ///     Arbitrary definition for when an explosion is large enough to require separating the area/tile-finding and
    ///     the processing into separate ticks.
    /// </summary>
    /// <remarks>
    ///     Only used when the explosion processing is not limited by time.
    /// </remarks>
    public const int NukeArea = 400;

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

                _explosionCounter++;
                _previousTileIteration = 0;

                // just a lil nap
                if (SleepNodeSys)
                {
                    _nodeGroupSystem.Snoozing = true;
                    // snooze grid-chunk regeneration?
                    // snooze power network (recipients look for new suppliers as wires get destroyed).
                }

                // if this is a single-tick explosion (i.e., not severely limited by number of tiles per tick or
                // processing time, we want to process large explosion on a tick separate from the one they were
                // generated on.
                if (_activeExplosion.Area > NukeArea
                    && MaxProcessingTime >= _gameTiming.TickPeriod.TotalMilliseconds)
                {
                    // start processing next turn.
                    break;
                }
            }

            var processed = _activeExplosion.Proccess(tilesRemaining);
            tilesRemaining -= processed;

            // has the explosion finished processing?
            if (_activeExplosion.FinishedProcessing)
                _activeExplosion = null;
        }

        Logger.InfoS("Explosion", $"Processed {TilesPerTick - tilesRemaining} tiles in {Stopwatch.Elapsed.TotalMilliseconds}ms");

        // we have finished processing our tiles. Is there still an ongoing explosion?
        if (_activeExplosion != null)
        {
            // update the client explosion overlays. This ensures that the fire-effects sync up with the entities currently being damaged.
            if (_previousTileIteration == _activeExplosion.CurrentIteration)
                return;

            _previousTileIteration = _activeExplosion.CurrentIteration;
            RaiseNetworkEvent(new ExplosionOverlayUpdateEvent(_explosionCounter, _previousTileIteration + 1));
            return;
        }

        if (_explosionQueue.Count > 0)
            return;

        // We have finished processing all explosions. Clear client explosion overlays
        RaiseNetworkEvent(new ExplosionOverlayUpdateEvent(_explosionCounter, int.MaxValue));

        //wakey wakey
        _nodeGroupSystem.Snoozing = false;
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

        if (!TryComp(uid, out IPhysBody? body))
            return false;

        return body.CanCollide && body.Hard && (body.CollisionLayer & (int) CollisionGroup.Impassable) != 0;
    }

    /// <summary>
    ///     Find entities on a grid tile using the EntityLookupComponent and apply explosion effects. 
    /// </summary>
    /// <returns>True if the underlying tile can be uprooted, false if the tile is blocked by a dense entity</returns>
    internal bool ExplodeTile(EntityLookupComponent lookup,
        IMapGrid grid,
        Vector2i tile,
        float intensity,
        float throwForce,
        DamageSpecifier damage,
        MapCoordinates epicenter,
        HashSet<EntityUid> processed,
        string id)
    {
        var gridBox = new Box2(tile * grid.TileSize, (tile + 1) * grid.TileSize);

        // get the entities on a tile. Note that we cannot process them directly, or we get
        // enumerator-changed-while-enumerating errors.
        List<EntityUid> list = new();
        _entityLookup.FastEntitiesIntersecting(lookup, ref gridBox, entity => list.Add(entity));

        // process those entities
        foreach (var entity in list)
        {
            ProcessEntity(entity, epicenter, processed, damage, throwForce, id, false);
        }

        // process anchored entities
        var tileBlocked = false;
        foreach (var entity in grid.GetAnchoredEntities(tile).ToList())
        {
            ProcessEntity(entity, epicenter, processed, damage, throwForce, id, true);
            tileBlocked |= IsBlockingTurf(entity);
        }

        // Next, we get the intersecting entities AGAIN, but purely for throwing. This way, glass shards spawned from
        // windows will be flung outwards, and not stay where they spawned. This is however somewhat unnecessary, and a
        // prime candidate for computational cost-cutting. Alternatively, it would be nice if there was just some sort
        // of spawned-on-destruction event that could be used to automatically assemble a list of new entities that need
        // to be thrown.
        //
        // All things considered, until entity spawning & destruction is sped up, this isn't all that time consuming.
        // (unless its a REALLY big explosion)
        if (throwForce <= 0)
            return !tileBlocked;

        list.Clear();
        _entityLookup.FastEntitiesIntersecting(lookup, ref gridBox, entity => list.Add(entity));

        foreach (var e in list)
        {
            // Here we only throw, no dealing damage. Containers n such might drop their entities after being destroyed, but
            // they handle their own damage pass-through.
            ProcessEntity(e, epicenter, processed, null, throwForce, id, false);
        }

        return !tileBlocked;
    }

    /// <summary>
    ///     Same as <see cref="ExplodeTile"/>, but for SPAAAAAAACE.
    /// </summary>
    internal void ExplodeSpace(EntityLookupComponent lookup,
        Matrix3 spaceMatrix,
        Matrix3 invSpaceMatrix,
        Vector2i tile,
        float intensity,
        float throwForce,
        DamageSpecifier damage,
        MapCoordinates epicenter,
        HashSet<EntityUid> processed,
        string id)
    {
        var gridBox = new Box2(tile * DefaultTileSize, (DefaultTileSize, DefaultTileSize));
        var worldBox = spaceMatrix.TransformBox(gridBox);
        List<EntityUid> list = new();

        EntityUidQueryCallback callback = uid =>
        {
            if (gridBox.Contains(invSpaceMatrix.Transform(Transform(uid).WorldPosition)))
                list.Add(uid);
        };

        _entityLookup.FastEntitiesIntersecting(lookup, ref worldBox, callback);

        foreach (var entity in list)
        {
            ProcessEntity(entity, epicenter, processed, damage, throwForce, id, false);
        }

        if (throwForce <= 0)
            return;

        list.Clear();
        _entityLookup.FastEntitiesIntersecting(lookup, ref worldBox, callback);
        foreach (var entity in list)
        {
            ProcessEntity(entity, epicenter, processed, null, throwForce, id, false);
        }
    }

    /// <summary>
    ///     This function actually applies the explosion affects to an entity.
    /// </summary>
    private void ProcessEntity(EntityUid uid, MapCoordinates epicenter, HashSet<EntityUid> processed, DamageSpecifier? damage, float throwForce, string id, bool anchored)
    {
        // check whether this is a valid target, and whether we have already damaged this entity (can happen with
        // explosion-throwing).
        if (!anchored && _containerSystem.IsEntityInContainer(uid) || !processed.Add(uid))
            return;

        // damage
        if (damage != null)
        {
            var ev = new GetExplosionResistanceEvent(id);
            RaiseLocalEvent(uid, ev, false);
            var coeff = Math.Clamp(0, 1 - ev.Resistance, 1);

            if (!MathHelper.CloseTo(0, coeff))
                _damageableSystem.TryChangeDamage(uid, damage * coeff, ignoreResistances: true);
        }

        // throw
        if (!anchored
            && throwForce > 0
            && !EntityManager.IsQueuedForDeletion(uid)
            && HasComp<ExplosionLaunchedComponent>(uid)
            && TryComp(uid, out TransformComponent? transform))
        {
            uid.TryThrow(transform.WorldPosition - epicenter.Position, throwForce);
        }

        // TODO EXPLOSION puddle / flammable ignite?

        // TODO EXPLOSION deaf/ear damage? other explosion effects?
    }

    /// <summary>
    ///     Tries to damage floor tiles. Not to be confused with the function that damages entities intersecting the
    ///     grid tile.
    /// </summary>
    public void DamageFloorTile(TileRef tileRef,
        float intensity,
        List<(Vector2i GridIndices, Tile Tile)> damagedTiles,
        ExplosionPrototype type)
    {
        var tileDef = _tileDefinitionManager[tileRef.Tile.TypeId];

        while (_robustRandom.Prob(type.TileBreakChance(intensity)))
        {
            intensity -= type.TileBreakRerollReduction;

            if (tileDef is not ContentTileDefinition contentTileDef)
                break;

            // does this have a base-turf that we can break it down to?
            if (contentTileDef.BaseTurfs.Count == 0)
                break;

            tileDef = _tileDefinitionManager[contentTileDef.BaseTurfs[^1]];
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
///     This is basically the output of <see cref="ExplosionSystem.GetExplosionTiles()"/>, but wrapped in an enumerator
///     to iterate over the tiles, along with the ability to keep track of what entities have already been damaged by
///     this explosion.
/// </remarks>
sealed class Explosion
{
    struct ExplosionData
    {
        public EntityLookupComponent Lookup;
        public Dictionary<int, List<Vector2i>> TileLists;
        public IMapGrid? MapGrid;
    }

    /// <summary>
    ///     Used to avoid applying explosion effects repeatedly to the same entity. Particularly important if the
    ///     explosion throws this entity, as then it will be moving while the explosion is happening.
    /// </summary>
    public readonly HashSet<EntityUid> ProcessedEntities = new();

    /// <summary>
    ///     This integer tracks how much of this explosion has been processed.
    /// </summary>
    public int CurrentIteration { get; private set; } = 0;

    public readonly ExplosionPrototype ExplosionType;
    public readonly MapCoordinates Epicenter;
    private readonly Matrix3 _spaceMatrix;
    private readonly Matrix3 _invSpaceMatrix;

    private readonly List<ExplosionData> _explosionData = new();
    private readonly List<float> _tileSetIntensity;

    public bool FinishedProcessing;

    // shitty enumerator implementation
    private DamageSpecifier _currentDamage = default!;
    private EntityLookupComponent _currentLookup = default!;
    private IMapGrid? _currentGrid;
    private float _currentIntensity;
    private float _currentThrowForce;
    private List<Vector2i>.Enumerator _currentEnumerator;
    private int _currentDataIndex;
    private Dictionary<IMapGrid, List<(Vector2i, Tile)>> _tileUpdateDict = new();

    public int Area;

    private readonly ExplosionSystem _system;

    public Explosion(ExplosionSystem system,
        ExplosionPrototype explosionType,
        SpaceExplosion? spaceData,
        List<GridExplosion> gridData,
        List<float> tileSetIntensity,
        MapCoordinates epicenter,
        Matrix3 spaceMatrix,
        int area)
    {
        _system = system;
        ExplosionType = explosionType;
        _tileSetIntensity = tileSetIntensity;
        Epicenter = epicenter;
        Area = area;

        var entityMan = IoCManager.Resolve<IEntityManager>();
        var mapMan = IoCManager.Resolve<IMapManager>();

        if (spaceData != null)
        {
            var mapUid = mapMan.GetMapEntityId(epicenter.MapId);

            _explosionData.Add(new()
            {
                TileLists = spaceData.TileLists,
                Lookup = entityMan.GetComponent<EntityLookupComponent>(mapUid),
                MapGrid = null
            });

            _spaceMatrix = spaceMatrix;
            _invSpaceMatrix = Matrix3.Invert(spaceMatrix);
        }

        foreach (var grid in gridData)
        {
            _explosionData.Add(new()
            {
                TileLists = grid.TileLists,
                Lookup = entityMan.GetComponent<EntityLookupComponent>(grid.Grid.GridEntityId),
                MapGrid = grid.Grid
            });
        }

        TryGetNextTileEnumerator();
    }

    private bool TryGetNextTileEnumerator()
    {
        while (CurrentIteration < _tileSetIntensity.Count)
        {
            _currentIntensity = _tileSetIntensity[CurrentIteration];
            _currentDamage = ExplosionType.DamagePerIntensity * _currentIntensity;
            _currentThrowForce = Area > _system.ThrowLimit ? 0 : 10 * MathF.Sqrt(_currentIntensity);

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
                return true;
            }

            // this explosion intensity has been fully processed, move to the next one
            CurrentIteration++;
            _currentDataIndex = 0;
        }

        // no more explosion data to process
        FinishedProcessing = true;
        return false;
    }

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

    public int Proccess(int processingTarget)
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

            if (_currentGrid != null &&
                _currentGrid.TryGetTileRef(_currentEnumerator.Current, out var tileRef) &&
                !tileRef.Tile.IsEmpty)
            {
                if (!_tileUpdateDict.TryGetValue(_currentGrid, out var tileUpdateList))
                {
                    tileUpdateList = new();
                    _tileUpdateDict[_currentGrid] = tileUpdateList;
                }

                var canDamageFloor = _system.ExplodeTile(_currentLookup,
                    _currentGrid,
                    _currentEnumerator.Current,
                    _currentIntensity,
                    _currentThrowForce,
                    _currentDamage,
                    Epicenter,
                    ProcessedEntities,
                    ExplosionType.ID);

                if (canDamageFloor)
                    _system.DamageFloorTile(tileRef, _currentIntensity, tileUpdateList, ExplosionType);
            }
            else
            {
                _system.ExplodeSpace(_currentLookup,
                    _spaceMatrix,
                    _invSpaceMatrix,
                    _currentEnumerator.Current,
                    _currentIntensity,
                    _currentThrowForce,
                    _currentDamage,
                    Epicenter,
                    ProcessedEntities,
                    ExplosionType.ID);
            }

            if (!MoveNext())
                break;
        }

        SetTiles();
        return processed;
    }

    private void SetTiles()
    {
        if (!_system.IncrementalTileBreaking && !FinishedProcessing)
            return;

        foreach (var (grid, list) in _tileUpdateDict)
        {
            if (list.Count > 0)
            {
                grid.SetTiles(list);
            }
        }
        _tileUpdateDict.Clear();
    }
}

