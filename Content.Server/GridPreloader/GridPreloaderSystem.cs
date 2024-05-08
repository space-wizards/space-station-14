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
using System.Numerics;
using Content.Server.GameTicking;
using Content.Shared.GameTicking;
using JetBrains.Annotations;

namespace Content.Server.GridPreloader;
public sealed class GridPreloaderSystem : SharedGridPreloaderSystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeLocalEvent<PostGameMapLoad>(OnPostGameMapLoad);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        var ent = GetPreloaderEntity();
        if (ent == null)
            return;

        Del(ent.Value.Owner);
    }

    private void OnPostGameMapLoad(PostGameMapLoad ev)
    {
        EnsurePreloadedGridMap();
    }

    private void EnsurePreloadedGridMap()
    {
        // Already have a preloader?
        if (GetPreloaderEntity() != null)
            return;

        if (!_cfg.GetCVar(CCVars.PreloadGrids))
            return;

        var mapUid = _map.CreateMap(out var mapId, false);
        var preloader = EnsureComp<GridPreloaderComponent>(mapUid);
        _meta.SetEntityName(mapUid, "GridPreloader Map");
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

                if (!_mapLoader.TryLoad(mapId, proto.Path.ToString(), out var roots, options))
                    continue;

                // only supports loading maps with one grid.
                if (roots.Count != 1)
                    continue;

                var gridUid = roots[0];

                // gets grid + also confirms that the root we loaded is actually a grid
                if (!TryComp<MapGridComponent>(gridUid, out var mapGrid))
                    continue;

                if (!TryComp<PhysicsComponent>(gridUid, out var physics))
                    continue;

                // Position Calculating
                globalXOffset += mapGrid.LocalAABB.Width / 2;

                var coords = new Vector2(-physics.LocalCenter.X + globalXOffset, -physics.LocalCenter.Y);
                _transform.SetCoordinates(gridUid, new EntityCoordinates(mapUid, coords));

                globalXOffset += (mapGrid.LocalAABB.Width / 2) + 1;

                // Add to list
                if (!preloader.PreloadedGrids.ContainsKey(proto.ID))
                    preloader.PreloadedGrids[proto.ID] = new();
                preloader.PreloadedGrids[proto.ID].Add(gridUid);
            }
        }
    }

    /// <summary>
    ///     Should be a singleton no matter station count, so we can assume 1
    ///     (better support for singleton component in engine at some point i guess)
    /// </summary>
    /// <returns></returns>
    public Entity<GridPreloaderComponent>? GetPreloaderEntity()
    {
        var query = AllEntityQuery<GridPreloaderComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            return (uid, comp);
        }

        return null;
    }

    /// <summary>
    /// An attempt to get a certain preloaded shuttle. If there are no more such shuttles left, returns null
    /// </summary>
    [PublicAPI]
    public bool TryGetPreloadedGrid(ProtoId<PreloadedGridPrototype> proto, [NotNullWhen(true)] out EntityUid? preloadedGrid, GridPreloaderComponent? preloader = null)
    {
        preloadedGrid = null;

        if (preloader == null)
        {
            preloader = GetPreloaderEntity();
            if (preloader == null)
                return false;
        }

        if (!preloader.PreloadedGrids.TryGetValue(proto, out var list) || list.Count <= 0)
            return false;

        preloadedGrid = list[0];

        list.RemoveAt(0);
        if (list.Count == 0)
            preloader.PreloadedGrids.Remove(proto);

        return true;
    }
}
