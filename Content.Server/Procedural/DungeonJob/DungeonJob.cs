using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Decals;
using Content.Server.NPC.Components;
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
    private readonly EntityTableSystem _entTable;
    private readonly TagSystem _tags;
    private readonly TileSystem _tile;
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
        DungeonConfig gen,
        MapGridComponent grid,
        EntityUid gridUid,
        int seed,
        Vector2i position,
        EntityCoordinates? targetCoordinates = null,
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
    /// <param name="reserve">Should we reserve tiles even if the config doesn't specify.</param>
    private async Task<List<Dungeon>> GetDungeons(
        Vector2i position,
        DungeonConfig config,
        List<IDunGenLayer> layers,
        HashSet<Vector2i> reservedTiles,
        int seed,
        Random random,
        List<Dungeon>? existing = null)
    {
        var dungeons = new List<Dungeon>();

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
                await RunLayer(dungeons, position, layer, reservedTiles, seed, random);

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
                    return new List<Dungeon>();
            }
        }

        return dungeons;
    }

    protected override async Task<List<Dungeon>?> Process()
    {
        _sawmill.Info($"Generating dungeon {_gen} with seed {_seed} on {_entManager.ToPrettyString(_gridUid)}");
        _grid.CanSplit = false;
        var random = new Random(_seed);
        var position = (_position + random.NextPolarVector2(_gen.MinOffset, _gen.MaxOffset)).Floored();

        // Tiles we can no longer generate on due to being reserved elsewhere.
        var reservedTiles = new HashSet<Vector2i>();

        var dungeons = await GetDungeons(position, _gen, _gen.Layers, reservedTiles, _seed, random);
        // To make it slightly more deterministic treat this RNG as separate ig.

        // Post-processing after finishing loading.
        if (_targetCoordinates != null)
        {
            var oldMap = _xformQuery.Comp(_gridUid).MapUid;
            _entManager.System<ShuttleSystem>().TryFTLProximity(_gridUid, _targetCoordinates.Value);
            _entManager.DeleteEntity(oldMap);
        }

        // Defer splitting so they don't get spammed and so we don't have to worry about tracking the grid along the way.
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
        return dungeons;
    }

    private async Task RunLayer(
        List<Dungeon> dungeons,
        Vector2i position,
        IDunGenLayer layer,
        HashSet<Vector2i> reservedTiles,
        int seed,
        Random random)
    {
        _sawmill.Debug($"Doing postgen {layer.GetType()} for {_gen} with seed {_seed}");

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
            case BiomeMarkerLayerDunGen markerPost:
                await PostGen(markerPost, dungeons[^1], reservedTiles, random);
                break;
            case BiomeDunGen biome:
                await PostGen(biome, dungeons[^1], reservedTiles, random);
                break;
            case BoundaryWallDunGen boundary:
                await PostGen(boundary, dungeons[^1], reservedTiles, random);
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
                dungeons.AddRange(await GenerateExteriorDungen(position, exterior, reservedTiles, random));
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

                switch (prototypo.InheritDungeons)
                {
                    case DungeonInheritance.All:
                        dungeons.AddRange(await GetDungeons(position, groupConfig, groupConfig.Layers, reservedTiles, seed, random, existing: dungeons));
                        break;
                    case DungeonInheritance.Last:
                        dungeons.AddRange(await GetDungeons(position, groupConfig, groupConfig.Layers, reservedTiles, seed, random, existing: dungeons.GetRange(dungeons.Count - 1, 1)));
                        break;
                    case DungeonInheritance.None:
                        dungeons.AddRange(await GetDungeons(position, groupConfig, groupConfig.Layers, reservedTiles, seed, random));
                        break;
                }

                break;
            case ReplaceTileDunGen replace:
                await GenerateTileReplacementDunGen(replace, dungeons, reservedTiles, random);
                break;
            case RoomEntranceDunGen rEntrance:
                await PostGen(rEntrance, dungeons[^1], reservedTiles, random);
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
