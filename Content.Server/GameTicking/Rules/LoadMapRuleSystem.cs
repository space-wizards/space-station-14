using System.Linq;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.GridPreloader;
using Content.Server.StationEvents.Events;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Robust.Server.GameObjects;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// Handler for rules that load maps when they're added.
/// </summary>
/// <seealso cref="LoadMapRuleComponent"/>
public sealed partial class LoadMapRuleSystem : StationEventSystem<LoadMapRuleComponent>
{
    [Dependency] private MapSystem _map = default!;
    [Dependency] private MapLoaderSystem _mapLoader = default!;
    [Dependency] private TransformSystem _transform = default!;
    [Dependency] private GridPreloaderSystem _gridPreloader = default!;

    protected override void Added(Entity<LoadMapRuleComponent, GameRuleComponent> ent, ref GameRuleAddedEvent args)
    {
        var loadMap = ent.Comp1;

#pragma warning disable CS0618 // Enabling compatibility behaviour, remove block when PreloadedGrid is removed.
        if (loadMap.PreloadedGrid != null && !_gridPreloader.PreloadingEnabled)
        {
            // Preloading will never work if it's disabled, duh
            Log.Debug($"Immediately ending {ToPrettyString(ent.Owner):rule} as preloading grids is disabled by cvar.");
            ForceEndSelf((ent.Owner, ent.Comp2));
            return;
        }
#pragma warning restore CS0618

        MapId mapId;
        IReadOnlyList<EntityUid> grids;
        if (loadMap.GameMap != null)
        {
            // Component has one of three modes, only one of the three fields should ever be populated.
            DebugTools.AssertNull(loadMap.MapPath);
            DebugTools.AssertNull(loadMap.GridPath);
            AssertPreloadedGridIsNull(loadMap);

            var gameMap = ProtoMan.Index(loadMap.GameMap.Value);
            grids = GameTicker.LoadGameMap(gameMap, out mapId, null);
            Log.Info($"Created map {mapId} for {ToPrettyString(ent.Owner):rule}");
        }
        else if (loadMap.MapPath is { } path)
        {
            DebugTools.AssertNull(loadMap.GridPath);
            AssertPreloadedGridIsNull(loadMap);

            var opts = DeserializationOptions.Default with { InitializeMaps = true };
            if (!_mapLoader.TryLoadMap(path, out var map, out var gridSet, opts))
            {
                Log.Error($"Failed to load map from {path}!");
                ForceEndSelf((ent.Owner, ent.Comp2));
                return;
            }

            grids = gridSet.Select(x => x.Owner).ToList();
            mapId = map.Value.Comp.MapId;
        }
        else if (loadMap.GridPath is { } gPath)
        {
            AssertPreloadedGridIsNull(loadMap);

            // I fucking love it when "map paths" choses to ar
            _map.CreateMap(out mapId);
            var opts = DeserializationOptions.Default with { InitializeMaps = true };
            if (!_mapLoader.TryLoadGrid(mapId, gPath, out var grid, opts))
            {
                Log.Error($"Failed to load grid from {gPath}!");
                ForceEndSelf((ent.Owner, ent.Comp2));
                return;
            }

            grids = new List<EntityUid> { grid.Value.Owner };
        }
#pragma warning disable CS0618 // Enabling compatibility behaviour, remove block when PreloadedGrid is removed.
        else if (loadMap.PreloadedGrid is { } preloaded)
#pragma warning restore CS0618
        {
            // TODO: If there are no preloaded grids left, any rule announcements will still go off!
            if (!_gridPreloader.TryGetPreloadedGrid(preloaded, out var loadedShuttle))
            {
                Log.Error($"Failed to get a preloaded grid with {preloaded}!");
                ForceEndSelf((ent.Owner, ent.Comp2));
                return;
            }

            var mapUid = _map.CreateMap(out mapId, runMapInit: false);
            _transform.SetParent(loadedShuttle.Value, mapUid);
            grids = new List<EntityUid>() { loadedShuttle.Value };
            _map.InitializeMap(mapUid);
        }
        else
        {
            Log.Error($"No valid map prototype or map path associated with the rule {ToPrettyString(ent.Owner)}");
            ForceEndSelf((ent.Owner, ent.Comp2));
            return;
        }

        var ev = new RuleLoadedGridsEvent(mapId, grids);
        RaiseLocalEvent(ent, ref ev);

        base.Added(ent, ref args);
    }

    /// <summary>
    /// Asserting that <paramref name="loadMap"/> has a null value for <see cref="LoadMapRuleComponent.PreloadedGrid"/>
    /// </summary>
    /// <remarks>
    /// Function to suppress warnings without injecting pragma statements everywhere.
    /// </remarks>
    private void AssertPreloadedGridIsNull(LoadMapRuleComponent loadMap)
    {
#pragma warning disable CS0618 // Enabling compatibility behaviour, remove block when PreloadedGrid is removed.
        DebugTools.AssertNull(loadMap.PreloadedGrid);
#pragma warning restore CS0618
    }
}
