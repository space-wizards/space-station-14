using Content.Server.GameTicking.Events;
using Content.Shared.GridPreloader.Prototypes;
using Content.Shared.GridPreloader.Systems;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
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

        var globalXOffset = 0f;
        foreach (var proto in _prototype.EnumeratePrototypes<PreloadedGridPrototype>())
        {
            for (int i = 0; i < proto.Copies; i++)
            {
                var options = new MapLoadOptions
                {
                    LoadMap = false,
                };

                // i dont use TryLoad, because he doesn't return EntityUid
                var gridUid = _map.LoadGrid(preloader.PreloadGridsMapId, proto.Path.ToString(), options);

                if (gridUid == null)
                    continue;

                EnsureComp<MapGridComponent>(gridUid.Value, out var mapGrid);
                EnsureComp<PhysicsComponent>(gridUid.Value, out var physic);

                //Position Calculating
                globalXOffset += mapGrid.LocalAABB.Width / 2;

                var xPos = -physic.LocalCenter.X + globalXOffset;
                var yPos = -physic.LocalCenter.Y;

                _transform.SetLocalPosition(Transform(gridUid.Value), new Vector2(xPos, yPos));
                _transform.SetLocalRotation(Transform(gridUid.Value), Angle.Zero);

                globalXOffset += (mapGrid.LocalAABB.Width / 2) + 1;

                //Add to list
                preloader.PreloadedGrids.Add(new(proto.ID, gridUid.Value));
            }
        }
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
