using Content.Server.GameTicking.Events;
using Content.Shared.GridPreloader.Prototypes;
using Content.Shared.GridPreloader.Systems;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using System.Linq;
using System.Numerics;

namespace Content.Server.GridPreloader;
public sealed partial class GridPreloaderSystem : SharedGridPreloaderSystem
{

    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly MapLoaderSystem _map = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;


    public override void Initialize()
    {
        base.Initialize();


        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
    }

    private void OnRoundStart(RoundStartingEvent ev)
    {
        //Init preloader entity in nullspace
        var preloaderEntity = Spawn();
        var preloader = AddComp<GridPreloaderComponent>(preloaderEntity);

        preloader.PreloadGridsMapId = _mapManager.CreateMap();
        _mapManager.AddUninitializedMap(preloader.PreloadGridsMapId);
        _mapManager.SetMapPaused(preloader.PreloadGridsMapId, true);

        var counter = 0;
        foreach (var grid in _prototype.EnumeratePrototypes<PreloadedGridPrototype>())
        {
            for (int i = 0; i < grid.Copies; i++)
            {
                CreateFrozenGrid(preloader, grid, counter);
                counter++;
            }
        }
    }

    private void CreateFrozenGrid(GridPreloaderComponent preloader, PreloadedGridPrototype proto, int counter)
    {
        if (proto.Path == null)
            return;

        var options = new MapLoadOptions
        {
            Offset = new Vector2(counter * 15, 0),
            LoadMap = false,
        };
        //dont use TryLoad, because he doesn't return EntityUid
        var gridUid = _map.LoadGrid(preloader.PreloadGridsMapId, proto.Path.ToString(), options);
        if (gridUid != null)
            preloader.PreloadedGrids.Add(new(proto.ID, gridUid.Value));
    }

    /// <summary>
    /// An attempt to get a certain preloaded shuttle. If there are no more such shuttles left, returns null
    /// </summary>
    public EntityUid? TryGetPreloadedGrid(ProtoId<PreloadedGridPrototype> proto, EntityCoordinates coord, GridPreloaderComponent? preloader = null)
    {
        if (preloader == null)
        {
            preloader = EntityQuery<GridPreloaderComponent>().FirstOrDefault();
            if (preloader == null)
                return null;
        }

        var shuttle = preloader.PreloadedGrids.Find(item => item.Item1 == proto);

        if (shuttle == default)
            return null;

        //Move Shuttle to map
        var uid = shuttle.Item2;

        _transform.SetCoordinates(uid, coord);
        preloader.PreloadedGrids.Remove(shuttle);
        return shuttle.Item2;
    }
}
