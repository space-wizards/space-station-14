using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Decals;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Server.Shuttles.Systems;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.EntityTable;
using Content.Shared.Maps;
using Content.Shared.Procedural;
using Content.Shared.Procedural.DungeonGenerators;
using Content.Shared.Procedural.DungeonLayers;
using Content.Shared.Procedural.PostGeneration;
using Content.Shared.Tag;
using JetBrains.Annotations;
using Robust.Server.Physics;
using Robust.Shared.CPUJob.JobQueues;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Procedural.DungeonJob;

public sealed partial class DungeonJob : Job<(List<Dungeon>, DungeonData)>
{
    public bool TimeSlice = true;

    private readonly IEntityManager _entManager;
    private readonly IPrototypeManager _prototype;
    private readonly ITileDefinitionManager _tileDefManager;

    private readonly AnchorableSystem _anchorable;
    private readonly DecalSystem _decals;
    private readonly DungeonSystem _dungeon;
    private readonly EntityLookupSystem _lookup;
    private readonly EntityTableSystem _entTable;
    private readonly TagSystem _tags;
    private readonly TileSystem _tile;
    private readonly TurfSystem _turf;
    private readonly SharedMapSystem _maps;
    private readonly SharedTransformSystem _transform;

    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityQuery<TransformComponent> _xformQuery;

    private readonly DungeonConfig _gen;
    private readonly int _seed;
    private readonly Vector2i _position;

    private readonly EntityUid _gridUid;
    private readonly MapGridComponent _grid;

    private readonly EntityCoordinates? _targetCoordinates;

    private readonly ISawmill _sawmill;

    private DungeonData _data = new();

    private HashSet<Vector2i>? _reservedTiles;

    public DungeonJob(
        ISawmill sawmill,
        double maxTime,
        IEntityManager entManager,
        IPrototypeManager prototype,
        ITileDefinitionManager tileDefManager,
        AnchorableSystem anchorable,
        DecalSystem decals,
        DungeonSystem dungeon,
        EntityLookupSystem lookup,
        TileSystem tile,
        TurfSystem turf,
        SharedTransformSystem transform,
        DungeonConfig gen,
        MapGridComponent grid,
        EntityUid gridUid,
        int seed,
        Vector2i position,
        EntityCoordinates? targetCoordinates = null,
        CancellationToken cancellation = default,
        HashSet<Vector2i>? reservedTiles = null) : base(maxTime, cancellation)
    {
        _sawmill = sawmill;
        _entManager = entManager;
        _prototype = prototype;
        _tileDefManager = tileDefManager;
        _reservedTiles = reservedTiles;

        _anchorable = anchorable;
        _decals = decals;
        _dungeon = dungeon;
        _lookup = lookup;
        _tile = tile;
        _turf = turf;
        _tags = _entManager.System<TagSystem>();
        _maps = _entManager.System<SharedMapSystem>();
        _entTable = _entManager.System<EntityTableSystem>();
        _transform = transform;

        _physicsQuery = _entManager.GetEntityQuery<PhysicsComponent>();
        _xformQuery = _entManager.GetEntityQuery<TransformComponent>();

        _gen = gen;
        _grid = grid;
        _gridUid = gridUid;
        _seed = seed;
        _position = position;
        _targetCoordinates = targetCoordinates;
    }

    /// <summary>
    /// Gets the relevant dungeon, running recursively as relevant.
    /// </summary>
    /// <param name="reservedTiles">Should we reserve tiles even if the config doesn't specify.</param>
    private async Task<(List<Dungeon>, HashSet<Vector2i>)> GetDungeons(
        Vector2i position,
        DungeonConfig config,
        List<IDunGenLayer> layers,
        int seed,
        Random random,
        HashSet<Vector2i>? reserved = null,
        List<Dungeon>? existing = null)
    {
        var dungeons = new List<Dungeon>();
        var reservedTiles = reserved == null ? new HashSet<Vector2i>() : new HashSet<Vector2i>(reserved);

        // Don't pass dungeons back up the "stack". They are ref types though it's a caller problem if they start trying to mutate it.
        if (existing != null)
        {
            dungeons.AddRange(existing);
        }

        var count = random.Next(config.MinCount, config.MaxCount + 1);

        for (var i = 0; i < count; i++)
        {
            position += random.NextPolarVector2(config.MinOffset, config.MaxOffset).Floored();

            foreach (var layer in layers)
            {
            	var dungCount = dungeons.Count;
                await RunLayer(i, count, config, dungeons, position, layer, reservedTiles, seed, random);

                if (config.ReserveTiles)
                {
                    // Reserve tiles on any new dungeons.
                    for (var d = dungCount; d < dungeons.Count; d++)
                    {
                        var dungeon = dungeons[d];
                        reservedTiles.UnionWith(dungeon.AllTiles);
                    }
                }

                await SuspendDungeon();
                if (!ValidateResume())
                    return (new List<Dungeon>(), new HashSet<Vector2i>());
            }
        }

        // Only return the new dungeons and applicable reserved tiles.
        return (dungeons[(existing?.Count ?? 0)..], config.ReturnReserved ? reservedTiles : new HashSet<Vector2i>());
    }

    protected override async Task<(List<Dungeon>, DungeonData)> Process()
    {
        _sawmill.Info($"Generating dungeon {_gen} with seed {_seed} on {_entManager.ToPrettyString(_gridUid)}");
        _grid.CanSplit = false;
        var random = new Random(_seed);
        var oldTileCount = _reservedTiles?.Count ?? 0;
        var position = (_position + random.NextPolarVector2(_gen.MinOffset, _gen.MaxOffset)).Floored();

        var (dungeons, _) = await GetDungeons(position, _gen, _gen.Layers, _seed, random, reserved: _reservedTiles);
        // To make it slightly more deterministic treat this RNG as separate ig.

        // Post-processing after finishing loading.
        if (_targetCoordinates != null)
        {
            var oldMap = _xformQuery.Comp(_gridUid).MapUid;
            _entManager.System<ShuttleSystem>().TryFTLProximity(_gridUid, _targetCoordinates.Value);
            _entManager.DeleteEntity(oldMap);
        }

        // Defer splitting so they don't get spammed and so we don't have to worry about tracking the grid along the way.
        DebugTools.Assert(oldTileCount == (_reservedTiles?.Count ?? 0));
        _grid.CanSplit = true;
        _entManager.System<GridFixtureSystem>().CheckSplits(_gridUid);
        var npcSystem = _entManager.System<NPCSystem>();
        var npcs = new HashSet<Entity<HTNComponent>>();

        _lookup.GetChildEntities(_gridUid, npcs);

        foreach (var npc in npcs)
        {
            npcSystem.WakeNPC(npc.Owner, npc.Comp);
        }

        _sawmill.Info($"Finished generating dungeon {_gen} with seed {_seed}");
        return (dungeons, _data);
    }

    private async Task RunLayer(
        int runCount,
        int maxRuns,
        DungeonConfig config,
        List<Dungeon> dungeons,
        Vector2i position,
        IDunGenLayer layer,
        HashSet<Vector2i> reservedTiles,
        int seed,
        Random random)
    {
        // _sawmill.Debug($"Doing postgen {layer.GetType()} for {_gen} with seed {_seed}");

        // If there's a way to just call the methods directly for the love of god tell me.
        // Some of these don't care about reservedtiles because they only operate on dungeon tiles (which should
        // never be reserved)

        // Some may or may not return dungeons.
        // It's clamplicated but yeah procgen layering moment I'll take constructive feedback.

        switch (layer)
        {
            case AutoCablingDunGen cabling:
                await PostGen(cabling, dungeons[^1], reservedTiles, random);
                break;
            case BoundaryWallDunGen boundary:
                await PostGen(boundary, dungeons[^1], reservedTiles, random);
                break;
            case ChunkDunGen chunk:
                dungeons.Add(await PostGen(chunk, reservedTiles, random));
                break;
            case CornerClutterDunGen clutter:
                await PostGen(clutter, dungeons[^1], reservedTiles, random);
                break;
            case CorridorClutterDunGen corClutter:
                await PostGen(corClutter, dungeons[^1], reservedTiles, random);
                break;
            case CorridorDunGen cordor:
                await PostGen(cordor, dungeons[^1], reservedTiles, random);
                break;
            case CorridorDecalSkirtingDunGen decks:
                await PostGen(decks, dungeons[^1], reservedTiles, random);
                break;
            case EntranceFlankDunGen flank:
                await PostGen(flank, dungeons[^1], reservedTiles, random);
                break;
            case ExteriorDunGen exterior:
                dungeons.AddRange(await GenerateExteriorDungen(runCount, maxRuns, position, exterior, reservedTiles, random));
                break;
            case FillGridDunGen fill:
                await GenerateFillDunGen(fill, dungeons, reservedTiles);
                break;
            case JunctionDunGen junc:
                await PostGen(junc, dungeons[^1], reservedTiles, random);
                break;
            case MiddleConnectionDunGen dordor:
                await PostGen(dordor, dungeons[^1], reservedTiles, random);
                break;
            case DungeonEntranceDunGen entrance:
                await PostGen(entrance, dungeons[^1], reservedTiles, random);
                break;
            case ExternalWindowDunGen externalWindow:
                await PostGen(externalWindow, dungeons[^1], reservedTiles, random);
                break;
            case InternalWindowDunGen internalWindow:
                await PostGen(internalWindow, dungeons[^1], reservedTiles, random);
                break;
            case MobsDunGen mob:
                await PostGen(mob, dungeons[^1], random);
                break;
            case EntityTableDunGen entityTable:
                await PostGen(entityTable, dungeons, reservedTiles, random);
                break;
            case NoiseDistanceDunGen distance:
                dungeons.Add(await GenerateNoiseDistanceDunGen(position, distance, reservedTiles, seed, random));
                break;
            case NoiseDunGen noise:
                dungeons.Add(await GenerateNoiseDunGen(position, noise, reservedTiles, seed, random));
                break;
            case OreDunGen ore:
                await PostGen(ore, dungeons, reservedTiles, random);
                break;
            case PrefabDunGen prefab:
                dungeons.Add(await GeneratePrefabDunGen(position, prefab, reservedTiles, random));
                break;
            case PrototypeDunGen prototypo:
                var groupConfig = _prototype.Index(prototypo.Proto);
                position = (position + random.NextPolarVector2(groupConfig.MinOffset, groupConfig.MaxOffset)).Floored();
                List<Dungeon>? inheritedDungeons = null;
                HashSet<Vector2i>? inheritedReserved = null;

                switch (prototypo.InheritReserved)
                {
                    case ReservedInheritance.All:
                        inheritedReserved = new HashSet<Vector2i>(reservedTiles);
                        break;
                    case ReservedInheritance.None:
                        break;
                    default:
                        throw new NotImplementedException();
                }

                switch (prototypo.InheritDungeons)
                {
                    case DungeonInheritance.All:
                        inheritedDungeons = dungeons;
                        break;
                    case DungeonInheritance.Last:
                        inheritedDungeons = dungeons.GetRange(dungeons.Count - 1, 1);
                        break;
                    case DungeonInheritance.None:
                        break;
                    default:
                        throw new NotImplementedException();
                }

                var (newDungeons, newReserved) = await GetDungeons(position,
                    groupConfig,
                    groupConfig.Layers,
                    seed,
                    random,
                    reserved: inheritedReserved,
                    existing: inheritedDungeons);
                dungeons.AddRange(newDungeons);

                if (groupConfig.ReturnReserved)
                {
                    reservedTiles.UnionWith(newReserved);
                }

                break;
            case ReplaceTileDunGen replace:
                await GenerateTileReplacementDunGen(replace, dungeons, reservedTiles, random);
                break;
            case RoofDunGen roof:
                await RoofGen(roof, dungeons, reservedTiles, random);
                break;
            case RoomEntranceDunGen rEntrance:
                await PostGen(rEntrance, dungeons[^1], reservedTiles, random);
                break;
            case SampleDecalDunGen sdec:
                await PostGen(sdec, dungeons, reservedTiles, random);
                break;
            case SampleEntityDunGen sent:
                await PostGen(sent, dungeons, reservedTiles, random);
                break;
            case SampleTileDunGen stile:
                await PostGen(stile, dungeons, reservedTiles, random);
                break;
            case SplineDungeonConnectorDunGen spline:
                dungeons.Add(await PostGen(spline, dungeons, reservedTiles, random));
                break;
            case WallMountDunGen wall:
                await PostGen(wall, dungeons[^1], reservedTiles, random);
                break;
            case WormCorridorDunGen worm:
                await PostGen(worm, dungeons[^1], reservedTiles, random);
                break;
            default:
                throw new NotImplementedException();
        }
    }

    [Pure]
    private bool ValidateResume()
    {
        if (_entManager.Deleted(_gridUid))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Wrapper around <see cref="Job{T}.SuspendIfOutOfTime"/>
    /// </summary>
    private async Task SuspendDungeon()
    {
        if (!TimeSlice)
            return;

        await SuspendIfOutOfTime();
    }

    private void AddLoadedEntity(Vector2i tile, EntityUid ent)
    {
        _data.Entities[ent] = tile;
    }

    private void AddLoadedDecal(Vector2 tile, uint decal)
    {
        _data.Decals[decal] = tile;
    }

    private void AddLoadedTile(Vector2i index, Tile tile)
    {
        _data.Tiles[index] = tile;
    }
}
