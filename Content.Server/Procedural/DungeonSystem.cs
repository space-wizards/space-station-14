using Content.Server.Decals;
using Content.Server.GameTicking.Events;
using Content.Shared.Procedural;
using Content.Shared.Procedural.DungeonGenerators;
using Content.Shared.Procedural.PostGeneration;
using Robust.Server.GameObjects;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Procedural;

public sealed partial class DungeonSystem : EntitySystem
{
    [Dependency] private readonly IConsoleHost _console = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefManager = default!;
    [Dependency] private readonly DecalSystem _decals = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MapLoaderSystem _loader = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = Logger.GetSawmill("dungen");
        _console.RegisterCommand("dungen", GenerateDungeon, CompletionCallback);
        _console.RegisterCommand("dungen_vis", VisualizeDungeon);
        _prototype.PrototypesReloaded += PrototypeReload;
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
    }

    private void OnRoundStart(RoundStartingEvent ev)
    {
        var query = AllEntityQuery<DungeonAtlasTemplateComponent>();

        while (query.MoveNext(out var uid, out _))
        {
            QueueDel(uid);
        }

        // Force all templates to be setup.
        foreach (var room in _prototype.EnumeratePrototypes<DungeonRoomPrototype>())
        {
            GetOrCreateTemplate(room);
        }
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _prototype.PrototypesReloaded -= PrototypeReload;
    }

    private void PrototypeReload(PrototypesReloadedEventArgs obj)
    {
        if (!obj.ByType.TryGetValue(typeof(DungeonRoomPrototype), out var rooms))
        {
            return;
        }

        foreach (var proto in rooms.Modified.Values)
        {
            var roomProto = (DungeonRoomPrototype) proto;
            var query = AllEntityQuery<DungeonAtlasTemplateComponent>();

            while (query.MoveNext(out var uid, out var comp))
            {
                if (!roomProto.AtlasPath.Equals(comp.Path))
                    continue;

                QueueDel(uid);
                break;
            }
        }

        foreach (var proto in rooms.Modified.Values)
        {
            var roomProto = (DungeonRoomPrototype) proto;
            var query = AllEntityQuery<DungeonAtlasTemplateComponent>();
            var found = false;

            while (query.MoveNext(out var comp))
            {
                if (!roomProto.AtlasPath.Equals(comp.Path))
                    continue;

                found = true;
                break;
            }

            if (!found)
            {
                GetOrCreateTemplate(roomProto);
            }
        }
    }

    private MapId GetOrCreateTemplate(DungeonRoomPrototype proto)
    {
        var query = AllEntityQuery<DungeonAtlasTemplateComponent>();
        DungeonAtlasTemplateComponent? comp;

        while (query.MoveNext(out var uid, out comp))
        {
            // Exists
            if (comp.Path?.Equals(proto.AtlasPath) == true)
                return Transform(uid).MapID;
        }

        var mapId = _mapManager.CreateMap();
        _loader.Load(mapId, proto.AtlasPath.ToString());
        comp = AddComp<DungeonAtlasTemplateComponent>(_mapManager.GetMapEntityId(mapId));
        comp.Path = proto.AtlasPath;
        return mapId;
    }

    private CompletionResult CompletionCallback(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(CompletionHelper.MapIds(EntityManager), "Map Id");
        }

        if (args.Length == 2)
        {
            return CompletionResult.FromHintOptions(CompletionHelper.PrototypeIDs<DungeonConfigPrototype>(proto: _prototype), $"Dungeon preset");
        }

        return CompletionResult.Empty;
    }

    public void GenerateDungeon(DungeonConfigPrototype gen, EntityUid gridUid, MapGridComponent grid, int seed)
    {
        Dungeon dungeon;
        _sawmill.Info($"Generating dungeon {gen.ID} with seed {seed} on {ToPrettyString(gridUid)}");

        switch (gen.Generator)
        {
            case PrefabDunGen prefab:
                dungeon = GeneratePrefabDungeon(prefab, gridUid, grid, seed);
                break;
            default:
                throw new NotImplementedException();
        }

        // To make it slightly more deterministic treat this RNG as separate ig.
        var random = new Random();

        foreach (var post in gen.PostGeneration)
        {
            switch (post)
            {
                case PoweredAirlockPostGen powair:
                    PostGen(powair, dungeon, random);
                    break;
                case EntrancePostGen entrance:
                    PostGen(entrance, dungeon, random);
                    break;
                case BoundaryWallPostGen boundary:
                    PostGen(boundary, dungeon, random);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }

    private Angle GetDungeonRotation(int seed)
    {
        // Mask 0 | 1 for rotation seed
        var dungeonRotationSeed = 3 & seed;
        return Math.PI / 2 * dungeonRotationSeed;
    }
}
