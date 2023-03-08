using System.Threading;
using System.Threading.Tasks;
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
        private IEntityManager _entManager;
        private IMapManager _mapManager;
        private IPrototypeManager _prototype;
        private ITileDefinitionManager _tileDefManager;

        private DecalSystem _decals;
        private DungeonSystem _dungeon;
        private EntityLookupSystem _lookup;
        private SharedTransformSystem _transform;

        private DungeonConfigPrototype _gen;
        private int _seed;
        private MapGridComponent _grid;
        private EntityUid _gridUid;

        public DungeonJob(
            double maxTime,
            IEntityManager entManager,
            IMapManager mapManager,
            IPrototypeManager prototype,
            ITileDefinitionManager tileDefManager,
            DecalSystem decals,
            DungeonSystem dungeon,
            EntityLookupSystem lookup,
            SharedTransformSystem transform,
            DungeonConfigPrototype gen,
            MapGridComponent grid,
            EntityUid gridUid,
            int seed,
            CancellationToken cancellation = default) : base(maxTime, cancellation)
        {
            _entManager = entManager;
            _mapManager = mapManager;
            _prototype = prototype;
            _tileDefManager = tileDefManager;

            _decals = decals;
            _dungeon = dungeon;
            _lookup = lookup;
            _transform = transform;

            _gen = gen;
            _grid = grid;
            _gridUid = gridUid;
            _seed = seed;
        }

        protected override async Task<Dungeon?> Process()
        {
            Dungeon dungeon;
            Logger.Info($"Generating dungeon {_gen.ID} with seed {_seed} on {_entManager.ToPrettyString(_gridUid)}");

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
            var random = new Random();

            foreach (var post in _gen.PostGeneration)
            {
                switch (post)
                {
                    case MiddleConnectionPostGen dordor:
                        await PostGen(dordor, dungeon, _grid, random);
                        break;
                    case EntrancePostGen entrance:
                        await PostGen(entrance, dungeon, _grid, random);
                        break;
                    case BoundaryWallPostGen boundary:
                        await PostGen(boundary, dungeon, _grid, random);
                        break;
                    default:
                        throw new NotImplementedException();
                }
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
