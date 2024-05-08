using System.Diagnostics.CodeAnalysis;
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
public sealed class GridPreloaderSystem : SharedGridPreloaderSystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
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

        var mapUid = _map.CreateMap(out var mapId, false);
        preloader.Comp.PreloadGridsMapId = mapId;
        _map.SetPaused(mapId, true);

        var globalXOffset = 0f;
        foreach (var proto in _prototype.EnumeratePrototypes<PreloadedGridPrototype>())
        {
            for (var i = 0; i < proto.Copies; i++)
            {
                var options = new MapLoadOptions
                {
                    LoadMap = false,
                };

                // i dont use TryLoad, because he doesn't return EntityUid
                // TODO MIRROR FIX
                var gridUid = _mapLoader.LoadGrid(preloader.Comp.PreloadGridsMapId.Value, proto.Path.ToString(), options);
                if (!TryComp<MapGridComponent>(gridUid, out var mapGrid))
                    continue;

                if (!TryComp<PhysicsComponent>(gridUid, out var physic))
                    continue;

                //Position Calculating
                globalXOffset += mapGrid.LocalAABB.Width / 2;

                var coords = new Vector2(-physic.LocalCenter.X + globalXOffset, -physic.LocalCenter.Y);
                _transform.SetCoordinates(gridUid.Value, new EntityCoordinates(mapUid, coords));

                globalXOffset += (mapGrid.LocalAABB.Width / 2) + 1;

                //Add to list
                if (!preloader.Comp.PreloadedGrids.ContainsKey(proto.ID))
                    preloader.Comp.PreloadedGrids[proto.ID] = new List<EntityUid>();
                preloader.Comp.PreloadedGrids[proto.ID].Add(gridUid.Value);
            }
        }
    }

    public GridPreloaderComponent? GetPreloaderEntity()
    {
        return EntityQuery<GridPreloaderComponent>().FirstOrDefault();
    }

    /// <summary>
    /// An attempt to get a certain preloaded shuttle. If there are no more such shuttles left, returns null
    /// </summary>
    public bool TryGetPreloadedGrid(ProtoId<PreloadedGridPrototype> proto, [NotNullWhen(true)] out EntityUid? preloadedGrid, GridPreloaderComponent? preloader = null)
    {
        preloadedGrid = null;

        if (preloader == null)
        {
            preloader = GetPreloaderEntity();
            if (preloader == null)
                return false;
        }

        if (preloader.PreloadedGrids.ContainsKey(proto) && preloader.PreloadedGrids[proto].Count > 0)
        {
            preloadedGrid = preloader.PreloadedGrids[proto][0];

            preloader.PreloadedGrids[proto].RemoveAt(0);
            if (preloader.PreloadedGrids[proto].Count == 0)
                preloader.PreloadedGrids.Remove(proto);

            return true;
        }

        return false;
    }
}
