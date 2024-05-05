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
using Robust.Shared.Collections;
using Robust.Shared.CPUJob.JobQueues;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Procedural.Job;

public sealed partial class DungeonJob : Job<ValueList<Dungeon>>
{
    public bool TimeSlice = true;

    private readonly IEntityManager _entManager;
    private readonly IPrototypeManager _prototype;
    private readonly ITileDefinitionManager _tileDefManager;

    private readonly AnchorableSystem _anchorable;
    private readonly DecalSystem _decals;
    private readonly DungeonSystem _dungeon;
    private readonly EntityLookupSystem _lookup;
    private readonly TileSystem _tile;
    private readonly SharedMapSystem _maps;
    private readonly SharedTransformSystem _transform;

    private EntityQuery<TransformComponent> _xformQuery;
    private EntityQuery<TagComponent> _tagQuery;

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
        _maps = _entManager.System<SharedMapSystem>();
        _transform = transform;

        _xformQuery = _entManager.GetEntityQuery<TransformComponent>();
        _tagQuery = _entManager.GetEntityQuery<TagComponent>();

        _gen = gen;
        _grid = grid;
        _gridUid = gridUid;
        _seed = seed;
        _position = position;
    }

    /// <summary>
    /// Gets the relevant dungeon, running recursively as relevant.
    /// </summary>
    private async Task<ValueList<Dungeon>> GetDungeon(
        Vector2i position,
        DungeonConfigPrototype config,
        IDunGen dungen,
        HashSet<Vector2i> reservedTiles,
        int seed)
    {
        var dungeons = new ValueList<Dungeon>();
        var rand = new Random(seed);

        var count = rand.Next(config.MinCount, config.MaxCount);

        for (var i = 0; i < count; i++)
        {
            Dungeon? dungeon = null;

            switch (dungen)
            {
                case ExteriorDunGen exterior:
                    dungeons.AddRange(await GenerateExteriorDungeon(position, exterior, reservedTiles, seed));
                    break;
                case FillGridDunGen fill:
                    await GenerateFillDungeon(fill, reservedTiles);
                    break;
                case GroupDunGen group:
                    for (var j = 0; j < group.Configs.Count; j++)
                    {
                        var groupConfig = _prototype.Index(group.Configs[j]);
                        position = (_position + rand.NextVector2(groupConfig.MinOffset, groupConfig.MaxOffset)).Floored();
                        dungeons.AddRange(await GetDungeon(position, groupConfig, groupConfig.Generator, reservedTiles, rand.Next()));
                    }

                    break;
                case NoiseDistanceDunGen distance:
                    dungeon = await GenerateNoiseDistanceDungeon(position, distance, reservedTiles, seed);
                    dungeons.Add(dungeon);
                    break;
                case NoiseDunGen noise:
                    dungeon = await GenerateNoiseDungeon(position, noise, reservedTiles, seed);
                    dungeons.Add(dungeon);
                    break;
                case PrefabDunGen prefab:
                    dungeon = await GeneratePrefabDungeon(position, prefab, reservedTiles, seed);
                    dungeons.Add(dungeon);
                    DebugTools.Assert(dungeon.RoomExteriorTiles.Count > 0);
                    break;
                default:
                    throw new NotImplementedException();
            }

            if (config.ReserveTiles)
            {
                reservedTiles.UnionWith(dungeons[^1].AllTiles);
            }

            if (dungeon != null)
            {
                // Run postgen on the dungeon.
                await PostGen(dungeon, config, reservedTiles, rand);
            }

            if (count > 1)
            {
                seed = rand.Next();
            }
        }

        return dungeons;
    }

    protected override async Task<ValueList<Dungeon>> Process()
    {
        _sawmill.Info($"Generating dungeon {_gen.ID} with seed {_seed} on {_entManager.ToPrettyString(_gridUid)}");
        _grid.CanSplit = false;
        var random = new Random(_seed);
        var position = (_position + random.NextVector2(_gen.MinOffset, _gen.MaxOffset)).Floored();

        // Tiles we can no longer generate on due to being reserved elsewhere.
        var reservedTiles = new HashSet<Vector2i>();

        var dungeons = await GetDungeon(position, _gen, _gen.Generator, reservedTiles, _seed);
        // To make it slightly more deterministic treat this RNG as separate ig.

        foreach (var dungeon in dungeons)
        {
            DebugTools.Assert(dungeon.RoomTiles.Count > 0);
        }

        // Defer splitting so they don't get spammed and so we don't have to worry about tracking the grid along the way.
        _grid.CanSplit = true;
        _entManager.System<GridFixtureSystem>().CheckSplits(_gridUid);
        return dungeons;
    }

    private async Task PostGen(Dungeon dungeon, DungeonConfigPrototype config, HashSet<Vector2i> reservedTiles, Random random)
    {
        foreach (var post in config.PostGeneration)
        {
            _sawmill.Debug($"Doing postgen {post.GetType()} for {_gen.ID} with seed {_seed}");

            // If there's a way to just call the methods directly for the love of god tell me.
            // Some of these don't care about reservedtiles because they only operate on dungeon tiles (which should
            // never be reserved)
            switch (post)
            {
                case AutoCablingPostGen cabling:
                    await PostGen(cabling, dungeon, reservedTiles, random);
                    break;
                case BiomePostGen biome:
                    await PostGen(biome, dungeon, random);
                    break;
                case BoundaryWallPostGen boundary:
                    await PostGen(boundary, dungeon, random);
                    break;
                case CornerClutterPostGen clutter:
                    await PostGen(clutter, dungeon, random);
                    break;
                case CorridorClutterPostGen corClutter:
                    await PostGen(corClutter, dungeon, random);
                    break;
                case CorridorPostGen cordor:
                    await PostGen(cordor, dungeon, reservedTiles, random);
                    break;
                case CorridorDecalSkirtingPostGen decks:
                    await PostGen(decks, dungeon);
                    break;
                case EntranceFlankPostGen flank:
                    await PostGen(flank, dungeon, reservedTiles, random);
                    break;
                case JunctionPostGen junc:
                    await PostGen(junc, dungeon, _gridUid, _grid, random);
                    break;
                case MiddleConnectionPostGen dordor:
                    await PostGen(dordor, dungeon, _gridUid, _grid, random);
                    break;
                case DungeonEntrancePostGen entrance:
                    await PostGen(entrance, dungeon, random);
                    break;
                case ExternalWindowPostGen externalWindow:
                    await PostGen(externalWindow, dungeon, _gridUid, _grid, random);
                    break;
                case InternalWindowPostGen internalWindow:
                    await PostGen(internalWindow, dungeon, _gridUid, _grid, random);
                    break;
                case BiomeMarkerLayerPostGen markerPost:
                    await PostGen(markerPost, dungeon, reservedTiles, random);
                    break;
                case RoomEntrancePostGen rEntrance:
                    await PostGen(rEntrance, dungeon, _gridUid, _grid, random);
                    break;
                case WallMountPostGen wall:
                    await PostGen(wall, dungeon, _gridUid, _grid, random);
                    break;
                case WormCorridorPostGen worm:
                    await PostGen(worm, dungeon, _gridUid, _grid, random);
                    break;
                default:
                    throw new NotImplementedException();
            }

        await SuspendDungeon();

        if (!ValidateResume())
            break;
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
}
