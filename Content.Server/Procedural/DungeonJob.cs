using System.Threading;
using System.Threading.Tasks;
using Content.Server.Construction;
using Robust.Shared.CPUJob.JobQueues;
using Content.Server.Decals;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.Maps;
using Content.Shared.Procedural;
using Content.Shared.Procedural.DungeonGenerators;
using Content.Shared.Procedural.PostGeneration;
using Content.Shared.Tag;
using Robust.Server.Physics;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Procedural;

public sealed partial class DungeonJob : Job<Dungeon>
{
    private readonly IEntityManager _entManager;
    private readonly IMapManager _mapManager;
    private readonly IPrototypeManager _prototype;
    private readonly ITileDefinitionManager _tileDefManager;

    private readonly AnchorableSystem _anchorable;
    private readonly DecalSystem _decals;
    private readonly DungeonSystem _dungeon;
    private readonly EntityLookupSystem _lookup;
    private readonly TileSystem _tile;
    private readonly SharedMapSystem _maps;
    private readonly SharedTransformSystem _transform;
    private EntityQuery<TagComponent> _tagQuery;

    private readonly DungeonConfigPrototype _gen;
    private readonly int _seed;
    private readonly Vector2i _position;

    private readonly MapGridComponent _grid;
    private readonly EntityUid _gridUid;

    private readonly ISawmill _sawmill;

    public DungeonJob(
        ISawmill sawmill,
        double maxTime,
        IEntityManager entManager,
        IMapManager mapManager,
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
        _mapManager = mapManager;
        _prototype = prototype;
        _tileDefManager = tileDefManager;

        _anchorable = anchorable;
        _decals = decals;
        _dungeon = dungeon;
        _lookup = lookup;
        _tile = tile;
        _maps = _entManager.System<SharedMapSystem>();
        _transform = transform;
        _tagQuery = _entManager.GetEntityQuery<TagComponent>();

        _gen = gen;
        _grid = grid;
        _gridUid = gridUid;
        _seed = seed;
        _position = position;
    }

    protected override async Task<Dungeon?> Process()
    {
        Dungeon dungeon;
        _sawmill.Info($"Generating dungeon {_gen.ID} with seed {_seed} on {_entManager.ToPrettyString(_gridUid)}");
        _grid.CanSplit = false;

        switch (_gen.Generator)
        {
            case NoiseDunGen noise:
                dungeon = await GenerateNoiseDungeon(noise, _gridUid, _grid, _seed);
                break;
            case PrefabDunGen prefab:
                dungeon = await GeneratePrefabDungeon(prefab, _gridUid, _grid, _seed);
                DebugTools.Assert(dungeon.RoomExteriorTiles.Count > 0);
                break;
            default:
                throw new NotImplementedException();
        }

        DebugTools.Assert(dungeon.RoomTiles.Count > 0);

        // To make it slightly more deterministic treat this RNG as separate ig.
        var random = new Random(_seed);

        foreach (var post in _gen.PostGeneration)
        {
            _sawmill.Debug($"Doing postgen {post.GetType()} for {_gen.ID} with seed {_seed}");

            switch (post)
            {
                case AutoCablingPostGen cabling:
                    await PostGen(cabling, dungeon, _gridUid, _grid, random);
                    break;
                case BiomePostGen biome:
                    await PostGen(biome, dungeon, _gridUid, _grid, random);
                    break;
                case BoundaryWallPostGen boundary:
                    await PostGen(boundary, dungeon, _gridUid, _grid, random);
                    break;
                case CornerClutterPostGen clutter:
                    await PostGen(clutter, dungeon, _gridUid, _grid, random);
                    break;
                case CorridorClutterPostGen corClutter:
                    await PostGen(corClutter, dungeon, _gridUid, _grid, random);
                    break;
                case CorridorPostGen cordor:
                    await PostGen(cordor, dungeon, _gridUid, _grid, random);
                    break;
                case CorridorDecalSkirtingPostGen decks:
                    await PostGen(decks, dungeon, _gridUid, _grid, random);
                    break;
                case EntranceFlankPostGen flank:
                    await PostGen(flank, dungeon, _gridUid, _grid, random);
                    break;
                case JunctionPostGen junc:
                    await PostGen(junc, dungeon, _gridUid, _grid, random);
                    break;
                case MiddleConnectionPostGen dordor:
                    await PostGen(dordor, dungeon, _gridUid, _grid, random);
                    break;
                case DungeonEntrancePostGen entrance:
                    await PostGen(entrance, dungeon, _gridUid, _grid, random);
                    break;
                case ExternalWindowPostGen externalWindow:
                    await PostGen(externalWindow, dungeon, _gridUid, _grid, random);
                    break;
                case InternalWindowPostGen internalWindow:
                    await PostGen(internalWindow, dungeon, _gridUid, _grid, random);
                    break;
                case BiomeMarkerLayerPostGen markerPost:
                    await PostGen(markerPost, dungeon, _gridUid, _grid, random);
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

            await SuspendIfOutOfTime();

            if (!ValidateResume())
                break;
        }

        // Defer splitting so they don't get spammed and so we don't have to worry about tracking the grid along the way.
        _grid.CanSplit = true;
        _entManager.System<GridFixtureSystem>().CheckSplits(_gridUid);
        return dungeon;
    }

    private bool ValidateResume()
    {
        if (_entManager.Deleted(_gridUid))
            return false;

        return true;
    }
}
