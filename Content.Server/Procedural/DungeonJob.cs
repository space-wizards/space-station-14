using System.Threading;
using System.Threading.Tasks;
using Content.Server.Construction;
using Content.Server.CPUJob.JobQueues;
using Content.Server.Decals;
using Content.Shared.Procedural;
using Content.Shared.Procedural.DungeonGenerators;
using Content.Shared.Procedural.PostGeneration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

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
    private readonly SharedTransformSystem _transform;

    private readonly DungeonConfigPrototype _gen;
    private readonly int _seed;
    private readonly Vector2 _position;

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
        SharedTransformSystem transform,
        DungeonConfigPrototype gen,
        MapGridComponent grid,
        EntityUid gridUid,
        int seed,
        Vector2 position,
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
        _transform = transform;

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

        switch (_gen.Generator)
        {
            case PrefabDunGen prefab:
                dungeon = await GeneratePrefabDungeon(prefab, _gridUid, _grid, _seed);
                break;
            default:
                throw new NotImplementedException();
        }

        foreach (var room in dungeon.Rooms)
        {
            dungeon.RoomTiles.UnionWith(room.Tiles);
        }

        // To make it slightly more deterministic treat this RNG as separate ig.
        var random = new Random(_seed);

        foreach (var post in _gen.PostGeneration)
        {
            _sawmill.Debug($"Doing postgen {post.GetType()} for {_gen.ID} with seed {_seed}");

            switch (post)
            {
                case MiddleConnectionPostGen dordor:
                    await PostGen(dordor, dungeon, _gridUid, _grid, random);
                    break;
                case EntrancePostGen entrance:
                    await PostGen(entrance, dungeon, _gridUid, _grid, random);
                    break;
                case ExternalWindowPostGen externalWindow:
                    await PostGen(externalWindow, dungeon, _gridUid, _grid, random);
                    break;
                case InternalWindowPostGen internalWindow:
                    await PostGen(internalWindow, dungeon, _gridUid, _grid, random);
                    break;
                case BoundaryWallPostGen boundary:
                    await PostGen(boundary, dungeon, _gridUid, _grid, random);
                    break;
                case WallMountPostGen wall:
                    await PostGen(wall, dungeon, _gridUid, _grid, random);
                    break;
                default:
                    throw new NotImplementedException();
            }

            await SuspendIfOutOfTime();
            ValidateResume();
        }

        return dungeon;
    }

    private bool ValidateResume()
    {
        if (_entManager.Deleted(_gridUid))
            return false;

        return true;
    }
}
