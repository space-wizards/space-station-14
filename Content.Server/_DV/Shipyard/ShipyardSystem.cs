using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared._DV.CCVars;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server._DV.Shipyard;

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
    public Entity<ShuttleComponent>? TryCreateShuttle(ResPath path)
    {
        if (!Enabled)
            return null;

        var map = _map.CreateMap(out var mapId);
        if (!_mapLoader.TryLoadGrid(mapId, path, out var grid))
        {
            Log.Error($"Failed to load shuttle {path}");
            Del(map);
            return null;
        }

        if (!TryComp<ShuttleComponent>(grid, out var comp))
        {
            Log.Error($"Shuttle {path}'s grid was missing ShuttleComponent");
            Del(map);
            return null;
        }

        _map.SetPaused(map, false);
        _mapDeleterShuttle.Enable(grid.Value);
        return (grid.Value, comp);
    }

    /// <summary>
    /// Adds a ship to the shipyard and attempts to ftl-dock it to the given station.
    /// </summary>
    public Entity<ShuttleComponent>? TrySendShuttle(Entity<StationDataComponent?> station, ResPath path)
    {
        if (!Resolve(station, ref station.Comp))
            return null;

        if (_station.GetLargestGrid(station.Comp) is not {} grid)
        {
            Log.Error($"Station {ToPrettyString(station):station} had no largest grid to FTL to");
            return null;
        }

        if (TryCreateShuttle(path) is not { } shuttle)
            return null;

        Log.Info($"Shuttle {path} was spawned for {ToPrettyString(station):station}, FTLing to {grid}");
        _shuttle.FTLToDock(shuttle, shuttle.Comp, grid, priorityTag: DockTag);
        return shuttle;
    }
}

