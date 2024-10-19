using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared._DeltaV.CCVars;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server._DeltaV.Shipyard;

/// <summary>
/// Handles spawning and ftling ships.
/// </summary>
public sealed class ShipyardSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly MapDeleterShuttleSystem _mapDeleterShuttle = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly ShuttleSystem _shuttle = default!;
    [Dependency] private readonly StationSystem _station = default!;

    [ValidatePrototypeId<TagPrototype>]
    public string DockTag = "DockShipyard";

    public bool Enabled;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_config, DCCVars.Shipyard, value => Enabled = value, true);
    }

    /// <summary>
    /// Creates a ship from its yaml path in the shipyard.
    /// </summary>
    public Entity<ShuttleComponent>? TryCreateShuttle(string path)
    {
        if (!Enabled)
            return null;

        var map = _map.CreateMap(out var mapId);
        _map.SetPaused(map, false);

        if (!_mapLoader.TryLoad(mapId, path, out var grids))
        {
            Log.Error($"Failed to load shuttle {path}");
            Del(map);
            return null;
        }

        // only 1 grid is supported, no tramshuttle
        if (grids.Count != 1)
        {
            var error = grids.Count < 1 ? "less" : "more";
            Log.Error($"Shuttle {path} had {error} than 1 grid, which is not supported.");
            Del(map);
            return null;
        }

        var uid = grids[0];
        if (!TryComp<ShuttleComponent>(uid, out var comp))
        {
            Log.Error($"Shuttle {path}'s grid was missing ShuttleComponent");
            Del(map);
            return null;
        }

        _mapDeleterShuttle.Enable(uid);
        return (uid, comp);
    }

    /// <summary>
    /// Adds a ship to the shipyard and attempts to ftl-dock it to the given station.
    /// </summary>
    public Entity<ShuttleComponent>? TrySendShuttle(Entity<StationDataComponent?> station, string path)
    {
        if (!Resolve(station, ref station.Comp))
            return null;

        if (_station.GetLargestGrid(station.Comp) is not {} grid)
        {
            Log.Error($"Station {ToPrettyString(station):station} had no largest grid to FTL to");
            return null;
        }

        if (TryCreateShuttle(path) is not {} shuttle)
            return null;

        Log.Info($"Shuttle {path} was spawned for {ToPrettyString(station):station}, FTLing to {grid}");
        _shuttle.FTLToDock(shuttle, shuttle.Comp, grid, priorityTag: DockTag);
        return shuttle;
    }
}
