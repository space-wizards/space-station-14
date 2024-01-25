using Content.Server.GameTicking.Events;
using Content.Shared.Shuttles.Prototypes;
using Content.Shared.Shuttles.Systems;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using System.Numerics;

namespace Content.Server.Shuttles.Systems;
public sealed partial class ShuttlePreloaderSystem : SharedShuttlePreloaderSystem
{

    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly MapLoaderSystem _map = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private MapId _shuttleMapId;
    private List<(string, EntityUid)> _preloadedShuttles = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
    }

    private void OnRoundStart(RoundStartingEvent ev)
    {
        _shuttleMapId = _mapManager.CreateMap();
        _mapManager.AddUninitializedMap(_shuttleMapId);
        _mapManager.SetMapPaused(_shuttleMapId, true);

        var counter = 0;
        foreach (var shuttle in _prototype.EnumeratePrototypes<PreloadedShuttlePrototype>())
        {
            for (int i = 0; i < shuttle.Copies; i++)
            {
                CreateFrozenShuttle(shuttle, counter);
                counter++;
            }
        }
    }

    private void CreateFrozenShuttle(PreloadedShuttlePrototype proto, int counter)
    {
        if (proto.Path == null)
            return;

        var options = new MapLoadOptions
        {
            Offset = new Vector2(counter * 15, 0),
            LoadMap = false,
        };
        //dont use TryLoad, because he doesn't return EntityUid
        var shuttle = _map.LoadGrid(_shuttleMapId, proto.Path, options);
        if (shuttle != null)
            _preloadedShuttles.Add(new(proto.ID, shuttle.Value));
    }

    /// <summary>
    /// An attempt to get a certain preloaded shuttle. If there are no more such shuttles left, returns null
    /// </summary>
    public EntityUid? TryGetPreloadedShuttle(ProtoId<PreloadedShuttlePrototype> proto, MapId map)
    {
        var shuttle = _preloadedShuttles.Find(item => item.Item1 == proto);

        if (shuttle == default)
            return null;

        //Move Shuttle to map
        var uid = shuttle.Item2;
        var mapId = _mapManager.GetMapEntityId(map);

        _transform.SetCoordinates(uid, Transform(mapId).Coordinates);
        _preloadedShuttles.Remove(shuttle);
        return shuttle.Item2;
    }
}
