using Content.Server.GameTicking.Events;
using Content.Shared.GridPreloader.Prototypes;
using Content.Shared.GridPreloader.Systems;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using System.Numerics;

namespace Content.Server.GridPreloader;
public sealed partial class GridPreloaderSystem : SharedGridPreloaderSystem
{

    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly MapLoaderSystem _map = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private MapId _preloadGridsMapId;
    private List<(string, EntityUid)> _preloadedGrids = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
    }

    private void OnRoundStart(RoundStartingEvent ev)
    {
        _preloadGridsMapId = _mapManager.CreateMap();
        _mapManager.AddUninitializedMap(_preloadGridsMapId);
        _mapManager.SetMapPaused(_preloadGridsMapId, true);

        var counter = 0;
        foreach (var grid in _prototype.EnumeratePrototypes<PreloadedGridPrototype>())
        {
            for (int i = 0; i < grid.Copies; i++)
            {
                CreateFrozenGrid(grid, counter);
                counter++;
            }
        }
    }

    private void CreateFrozenGrid(PreloadedGridPrototype proto, int counter)
    {
        if (proto.Path == null)
            return;

        var options = new MapLoadOptions
        {
            Offset = new Vector2(counter * 15, 0),
            LoadMap = false,
        };
        //dont use TryLoad, because he doesn't return EntityUid
        var gridUid = _map.LoadGrid(_preloadGridsMapId, proto.Path, options);
        if (gridUid != null)
            _preloadedGrids.Add(new(proto.ID, gridUid.Value));
    }

    /// <summary>
    /// An attempt to get a certain preloaded shuttle. If there are no more such shuttles left, returns null
    /// </summary>
    public EntityUid? TryGetPreloadedGrid(ProtoId<PreloadedGridPrototype> proto, EntityCoordinates coord)
    {
        var shuttle = _preloadedGrids.Find(item => item.Item1 == proto);

        if (shuttle == default)
            return null;

        //Move Shuttle to map
        var uid = shuttle.Item2;

        _transform.SetCoordinates(uid, coord);
        _preloadedGrids.Remove(shuttle);
        return shuttle.Item2;
    }
}
