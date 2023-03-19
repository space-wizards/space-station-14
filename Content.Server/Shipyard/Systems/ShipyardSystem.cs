using Content.Server.Shuttles.Systems;
using Content.Server.Shuttles.Components;
using Content.Server.Station.Components;
using Content.Server.Cargo.Systems;
using Content.Server.Station.Systems;
using Content.Shared.Shipyard.Components;
using Content.Shared.Shipyard;
using Content.Shared.GameTicking;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Map;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Coordinates;
using Content.Shared.Shipyard.Events;
using Content.Shared.Mobs.Components;
using Robust.Shared.Containers;

namespace Content.Server.Shipyard.Systems;

public sealed partial class ShipyardSystem : SharedShipyardSystem
{
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly CargoSystem _cargo = default!;
    [Dependency] private readonly PricingSystem _pricing = default!;
    [Dependency] private readonly ShuttleSystem _shuttle = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly MapLoaderSystem _map = default!;

    public MapId? ShipyardMap { get; private set; }
    private float _shuttleIndex;
    private const float ShuttleSpawnBuffer = 1f;
    private ISawmill _sawmill = default!;
    private bool _enabled;

    public override void Initialize()
    {
        base.Initialize();
        _enabled = _configManager.GetCVar(CCVars.Shipyard);
        _configManager.OnValueChanged(CCVars.Shipyard, SetShipyardEnabled, true);
        _sawmill = Logger.GetSawmill("shipyard");

        SubscribeLocalEvent<ShipyardConsoleComponent, ComponentStartup>(OnShipyardStartup);
        SubscribeLocalEvent<ShipyardConsoleComponent, BoundUIOpenedEvent>(OnConsoleUIOpened);
        SubscribeLocalEvent<ShipyardConsoleComponent, ShipyardConsoleSellMessage>(OnSellMessage);
        SubscribeLocalEvent<ShipyardConsoleComponent, ShipyardConsolePurchaseMessage>(OnPurchaseMessage);
        SubscribeLocalEvent<ShipyardConsoleComponent, EntInsertedIntoContainerMessage>(OnItemSlotChanged);
        SubscribeLocalEvent<ShipyardConsoleComponent, EntRemovedFromContainerMessage>(OnItemSlotChanged);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }
    public override void Shutdown()
    {
        _configManager.UnsubValueChanged(CCVars.Shipyard, SetShipyardEnabled);
    }
    private void OnShipyardStartup(EntityUid uid, ShipyardConsoleComponent component, ComponentStartup args)
    {
        if (!_enabled)
            return;
        InitializeConsole();
        SetupShipyard();
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        CleanupShipyard();
    }

    private void SetShipyardEnabled(bool value)
    {
        if (_enabled == value)
            return;

        _enabled = value;

        if (value)
        {
            SetupShipyard();
        }
        else
        {
            CleanupShipyard();
        }
    }

    /// <summary>
    /// Adds a ship to the shipyard, calculates its price, and attempts to ftl-dock it to the given station
    /// </summary>
    /// <param name="stationUid">The ID of the station to dock the shuttle to</param>
    /// <param name="shuttlePath">The path to the shuttle file to load. Must be a grid file!</param>
    public bool TryPurchaseShuttle(EntityUid stationUid, string shuttlePath, [NotNullWhen(true)] out ShuttleComponent? shuttle)
    {
        if (!TryComp<StationDataComponent>(stationUid, out var stationData) || !TryAddShuttle(shuttlePath, out var shuttleGrid) || !TryComp<ShuttleComponent>(shuttleGrid, out shuttle))
        {
            shuttle = null;
            return false;
        }

        var price = _pricing.AppraiseGrid((EntityUid) shuttleGrid, null);
        var targetGrid = _station.GetLargestGrid(stationData);


        if (targetGrid == null) //how are we even here with no station grid
        {
            _mapManager.DeleteGrid((EntityUid) shuttleGrid);
            shuttle = null;
            return false;
        }

        _sawmill.Info($"Shuttle {shuttlePath} was purchased at {ToPrettyString((EntityUid) stationUid)} for {price:f2}");
        //can do TryFTLDock later instead if we need to keep the shipyard map paused
        _shuttle.FTLTravel(shuttle, targetGrid.Value, 0f, 30f, true);

        return true;
    }

    /// <summary>
    /// Loads a shuttle into the ShipyardMap from a file path
    /// </summary>
    /// <param name="shuttlePath">The path to the grid file to load. Must be a grid file!</param>
    /// <returns>Returns the EntityUid of the shuttle</returns>
    private bool TryAddShuttle(string shuttlePath, [NotNullWhen(true)] out EntityUid? shuttleGrid)
    {
        shuttleGrid = null;
        if (ShipyardMap == null)
            return false;

        var loadOptions = new MapLoadOptions()
        {
            Offset = (500f + _shuttleIndex, 1f)
        };

        if (!_map.TryLoad(ShipyardMap.Value, shuttlePath, out var gridList, loadOptions))
        {
            _sawmill.Error($"Unable to spawn shuttle {shuttlePath}");
            return false;
        };

        _shuttleIndex += _mapManager.GetGrid(gridList[0]).LocalAABB.Width + ShuttleSpawnBuffer;

        //only dealing with 1 grid at a time for now, until more is known about multi-grid drifting
        if (gridList.Count != 1)
        {
            if (gridList.Count < 1)
            {
                _sawmill.Error($"Unable to spawn shuttle {shuttlePath}, no grid found in file");
            };

            if (gridList.Count > 1)
            {
                _sawmill.Error($"Unable to spawn shuttle {shuttlePath}, too many grids present in file");

                foreach (var grid in gridList)
                {
                    _mapManager.DeleteGrid(grid);
                };
            };

            return false;
        };

        shuttleGrid = gridList[0];
        return true;
    }

    /// <summary>
    /// Checks a shuttle to make sure that it is docked to the given station, and that there are no lifeforms aboard. Then it appraises the grid, outputs to the server log, and deletes the grid
    /// </summary>
    /// <param name="stationUid">The ID of the station that the shuttle is docked to</param>
    /// <param name="shuttleUid">The grid ID of the shuttle to be appraised and sold</param>
    public bool TrySellShuttle(EntityUid stationUid, EntityUid shuttleUid, out int bill)
    {
        bill = 0;

        if (!TryComp<StationDataComponent>(stationUid, out var stationGrid) || !HasComp<ShuttleComponent>(shuttleUid) || !TryComp<TransformComponent>(shuttleUid, out var xform) || ShipyardMap == null)
            return false;

        var targetGrid = _station.GetLargestGrid(stationGrid);

        if (targetGrid == null)
            return false;

        var gridDocks = _shuttle.GetDocks((EntityUid) targetGrid);
        var shuttleDocks = _shuttle.GetDocks(shuttleUid);
        var isDocked = false;

        foreach (var shuttleDock in shuttleDocks)
        {
            foreach (var gridDock in gridDocks)
            {
                if (shuttleDock.DockedWith == gridDock.Owner)
                {
                    isDocked = true;
                    break;
                };
            };
            if (isDocked)
                break;
        };

        if (!isDocked)
        {
            _sawmill.Warning($"shuttle is not docked to that station");
            return false;
        };

        var mobQuery = GetEntityQuery<MobStateComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();

        if (_cargo.FoundOrganics(shuttleUid, mobQuery, xformQuery))
        {
            _sawmill.Warning($"organics on board");
            return false;
        };

        //just yeet and delete for now. Might want to split it into another function later to send back to the shipyard map first to pause for something
        //also superman 3 moment
        bill = (int) _pricing.AppraiseGrid(shuttleUid);
        _mapManager.DeleteGrid(shuttleUid);
        _sawmill.Info($"Sold shuttle {shuttleUid} for {bill}");
        return true;
    }

    private void CleanupShipyard()
    {
        if (ShipyardMap == null || !_mapManager.MapExists(ShipyardMap.Value))
        {
            ShipyardMap = null;
            return;
        };

        _mapManager.DeleteMap(ShipyardMap.Value);
    }

    private void SetupShipyard()
    {
        if (ShipyardMap != null && _mapManager.MapExists(ShipyardMap.Value))
            return;

        ShipyardMap = _mapManager.CreateMap();

        _mapManager.SetMapPaused(ShipyardMap.Value, false);
    }
}
