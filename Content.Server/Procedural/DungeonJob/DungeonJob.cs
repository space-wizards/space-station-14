using System.Threading;
using System.Threading.Tasks;
using Content.Server.Decals;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.Maps;
using Content.Shared.Procedural;
using Content.Shared.Procedural.DungeonGenerators;
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
using IDunGenLayer = Content.Shared.Procedural.IDunGenLayer;

namespace Content.Server.Procedural.DungeonJob;

public sealed partial class DungeonJob : Job<List<Dungeon>>
{
    public bool TimeSlice = true;

    private readonly IEntityManager _entManager;
    private readonly IPrototypeManager _prototype;
    private readonly ITileDefinitionManager _tileDefManager;

    private readonly AnchorableSystem _anchorable;
    private readonly DecalSystem _decals;
    private readonly DungeonSystem _dungeon;
    private readonly EntityLookupSystem _lookup;
    private readonly TagSystem _tags;
    private readonly TileSystem _tile;
    private readonly SharedMapSystem _maps;
    private readonly SharedTransformSystem _transform;

    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityQuery<TransformComponent> _xformQuery;

    private readonly DungeonConfigPrototype _gen;
    private readonly int _seed;
    private readonly Vector2i _position;

    private readonly EntityUid _gridUid;
    private readonly MapGridComponent _grid;

    private readonly ISawmill _sawmill;

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
        SharedTransformSystem transform,
        DungeonConfigPrototype gen,
        MapGridComponent grid,
        EntityUid gridUid,
        int seed,
        Vector2i position,
        CancellationToken cancellation = default) : base(maxTime, cancellation)
    {
        _sawmill = sawmill;
        _entManager = entManager;
        _prototype = prototype;
        _tileDefManager = tileDefManager;

        _anchorable = anchorable;
        _decals = decals;
        _dungeon = dungeon;
        _lookup = lookup;
        _tile = tile;
        _tags = _entManager.System<TagSystem>();
        _maps = _entManager.System<SharedMapSystem>();
        _transform = transform;

        _physicsQuery = _entManager.GetEntityQuery<PhysicsComponent>();
        _xformQuery = _entManager.GetEntityQuery<TransformComponent>();

        _gen = gen;
        _grid = grid;
        _gridUid = gridUid;
        _seed = seed;
        _position = position;
    }

    /// <summary>
    /// Gets the relevant dungeon, running recursively as relevant.
    /// </summary>
    /// <param name="reserve">Should we reserve tiles even if the config doesn't specify.</param>
    private async Task<List<Dungeon>> GetDungeons(
        Vector2i position,
        DungeonConfigPrototype config,
        DungeonData data,
        List<IDunGenLayer> layers,
        HashSet<Vector2i> reservedTiles,
        int seed)
    {
        var dungeons = new List<Dungeon>();
        var rand = new Random(seed);
        var count = rand.Next(config.MinCount, config.MaxCount);

        for (var i = 0; i < count; i++)
        {
            position += rand.NextPolarVector2(config.MinOffset, config.MaxOffset).Floored();

            foreach (var layer in layers)
            {
                await RunLayer(dungeons, data, position, layer, reservedTiles, seed);

                if (config.ReserveTiles)
                {
                    foreach (var dungeon in dungeons)
                    {
                        reservedTiles.UnionWith(dungeon.AllTiles);
                    }
                }

                await SuspendDungeon();
                if (!ValidateResume())
                    return new List<Dungeon>();
            }

            seed = rand.Next();
        }

        return dungeons;
    }

    protected override async Task<List<Dungeon>?> Process()
    {
        _sawmill.Info($"Generating dungeon {_gen.ID} with seed {_seed} on {_entManager.ToPrettyString(_gridUid)}");
        _grid.CanSplit = false;
        var random = new Random(_seed);
        var position = (_position + random.NextPolarVector2(_gen.MinOffset, _gen.MaxOffset)).Floored();

        // Tiles we can no longer generate on due to being reserved elsewhere.
        var reservedTiles = new HashSet<Vector2i>();

        var dungeons = await GetDungeons(position, _gen, _gen.Data, _gen.Layers, reservedTiles, _seed);
        // To make it slightly more deterministic treat this RNG as separate ig.

        // Defer splitting so they don't get spammed and so we don't have to worry about tracking the grid along the way.
        _grid.CanSplit = true;
        _entManager.System<GridFixtureSystem>().CheckSplits(_gridUid);
        return dungeons;
    }

    private async Task RunLayer(List<Dungeon> dungeons, DungeonData data, Vector2i position, IDunGenLayer layer, HashSet<Vector2i> reservedTiles, int seed)
    {
        _sawmill.Debug($"Doing postgen {layer.GetType()} for {_gen.ID} with seed {_seed}");

        // If there's a way to just call the methods directly for the love of god tell me.
        // Some of these don't care about reservedtiles because they only operate on dungeon tiles (which should
        // never be reserved)
        var random = new Random(seed);

        switch (layer)
        {
            // Dungeon generators
            case ExteriorDunGen exterior:
                dungeons.AddRange(await GenerateExteriorDungeon(position, data, exterior, reservedTiles, seed));
                break;
            case FillGridDunGen fill:
                await GenerateFillDungeon(position, data, fill, reservedTiles, seed);
                break;
            case NoiseDistanceDunGen distance:
                dungeons.Add(await GenerateNoiseDistanceDungeon(position, data, distance, reservedTiles, seed));
                break;
            case NoiseDunGen noise:
                dungeons.Add(await GenerateNoiseDungeon(position, data, noise, reservedTiles, seed));
                break;
            case PrototypeDunGen prototypo:
                var groupConfig = _prototype.Index(prototypo.Proto);
                position = (position + random.NextPolarVector2(groupConfig.MinOffset, groupConfig.MaxOffset)).Floored();

                var dataCopy = groupConfig.Data.Clone();
                dataCopy.Apply(data);

                dungeons.AddRange(await GetDungeons(position, groupConfig, dataCopy, groupConfig.Layers, reservedTiles, seed));
                break;
            case PrefabDunGen prefab:
                dungeons.Add(await GeneratePrefabDungeon(position, data, prefab, reservedTiles, seed));
                break;

            case ReplaceTileDunGen replace:
                dungeons.Add(await GenerateTileReplacementDungeon(replace, data, reservedTiles, random));
                break;

            // Postgen
            case AutoCablingPostGen cabling:
                await PostGen(cabling, data, dungeons[^1], reservedTiles, random);
                break;
            case BiomePostGen biome:
                await PostGen(biome, data, dungeons[^1], reservedTiles, random);
                break;
            case BoundaryWallPostGen boundary:
                await PostGen(boundary, data, dungeons[^1], reservedTiles, random);
                break;
            case CornerClutterPostGen clutter:
                await PostGen(clutter, data, dungeons[^1], reservedTiles, random);
                break;
            case CorridorClutterPostGen corClutter:
                await PostGen(corClutter, data, dungeons[^1], reservedTiles, random);
                break;
            case CorridorPostGen cordor:
                await PostGen(cordor, data, dungeons[^1], reservedTiles, random);
                break;
            case CorridorDecalSkirtingPostGen decks:
                await PostGen(decks, data, dungeons[^1], reservedTiles, random);
                break;
            case EntranceFlankPostGen flank:
                await PostGen(flank, data, dungeons[^1], reservedTiles, random);
                break;
            case JunctionPostGen junc:
                await PostGen(junc, data, dungeons[^1], reservedTiles, random);
                break;
            case MiddleConnectionPostGen dordor:
                await PostGen(dordor, data, dungeons[^1], reservedTiles, random);
                break;
            case DungeonEntrancePostGen entrance:
                await PostGen(entrance, data, dungeons[^1], reservedTiles, random);
                break;
            case ExternalWindowPostGen externalWindow:
                await PostGen(externalWindow, data, dungeons[^1], reservedTiles, random);
                break;
            case InternalWindowPostGen internalWindow:
                await PostGen(internalWindow, data, dungeons[^1], reservedTiles, random);
                break;
            case BiomeMarkerLayerPostGen markerPost:
                await PostGen(markerPost, data, dungeons[^1], reservedTiles, random);
                break;
            case RoomEntrancePostGen rEntrance:
                await PostGen(rEntrance, data, dungeons[^1], reservedTiles, random);
                break;
            case WallMountPostGen wall:
                await PostGen(wall, data, dungeons[^1], reservedTiles, random);
                break;
            case WormCorridorPostGen worm:
                await PostGen(worm, data, dungeons[^1], reservedTiles, random);
                break;
            default:
                throw new NotImplementedException();
        }
    }

    private void LogDataError(Type type)
    {
        _sawmill.Error($"Unable to find dungeon data keys for {type}");
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
}
