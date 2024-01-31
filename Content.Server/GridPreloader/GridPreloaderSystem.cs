using Content.Shared.CCVar;
using Content.Shared.GridPreloader.Prototypes;
using Content.Shared.GridPreloader.Systems;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using System.Linq;
using System.Numerics;

namespace Content.Server.GridPreloader;
public sealed partial class GridPreloaderSystem : SharedGridPreloaderSystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly MapLoaderSystem _map = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GridPreloaderComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<GridPreloaderComponent> preloader, ref MapInitEvent args)
    {
        if (!_cfg.GetCVar(CCVars.PreloadGrids))
            return;

        preloader.Comp.PreloadGridsMapId = _mapManager.CreateMap();
        _mapManager.AddUninitializedMap(preloader.Comp.PreloadGridsMapId.Value);
        _mapManager.SetMapPaused(preloader.Comp.PreloadGridsMapId.Value, true);

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
                var gridUid = _map.LoadGrid(preloader.Comp.PreloadGridsMapId.Value, proto.Path.ToString(), options);

                if (!TryComp<MapGridComponent>(gridUid, out var mapGrid))
                    continue;

                if (!TryComp<PhysicsComponent>(gridUid, out var physic))
                    continue;

                if (gridUid == null)
                    continue;

                //Position Calculating
                globalXOffset += mapGrid.LocalAABB.Width / 2;

                var xform = Transform(gridUid.Value);

                var coord = new Vector2(-physic.LocalCenter.X + globalXOffset, -physic.LocalCenter.Y);

                _transform.SetLocalPosition(xform, coord);
                _transform.SetLocalRotation(xform, Angle.Zero);

                globalXOffset += (mapGrid.LocalAABB.Width / 2) + 1;

                //Add to list
                preloader.Comp.PreloadedGrids.Add(new(proto.ID, gridUid.Value));
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

        if (shuttle.Item2 == null)
            return null;

        //Move Shuttle to map
        var uid = shuttle.Item2.Value;

        _transform.SetCoordinates(uid, coord);
        preloader.PreloadedGrids.Remove(shuttle);
        return shuttle.Item2;
    }
}
