using Content.Server.Shuttles.Systems;
using Content.Server.Shuttles.Components;
using Content.Server.Station.Components;
using Content.Server.Cargo.Systems;
using Content.Server.Station.Systems;
using Content.Shared.Shipyard;
using Content.Server.Shipyard.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.GameTicking;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Content.Server.Cargo.Components;

namespace Content.Server.Shipyard.Systems
{

    public sealed partial class ShipyardSystem : SharedShipyardSystem
    {
        [Dependency] private readonly IConfigurationManager _configManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly PricingSystem _pricing = default!;
        [Dependency] private readonly ShuttleSystem _shuttle = default!;
        [Dependency] private readonly StationSystem _station = default!;
        [Dependency] private readonly MapLoaderSystem _map = default!;
        [Dependency] private readonly ShipyardConsoleSystem _shipyardConsole = default!;

        public MapId? ShipyardMap { get; private set; }
        private float _shuttleIndex;
        private const float ShuttleSpawnBuffer = 1f;
        private ISawmill _sawmill = default!;
        private bool _enabled;

        public override void Initialize()
        {
            _enabled = _configManager.GetCVar(CCVars.Shipyard);
            _configManager.OnValueChanged(CCVars.Shipyard, SetShipyardEnabled);
            _sawmill = Logger.GetSawmill("shipyard");
            _shipyardConsole.InitializeConsole();
            SubscribeLocalEvent<ShipyardConsoleComponent, ComponentInit>(OnShipyardStartup);
            SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        }

        private void OnShipyardStartup(EntityUid uid, ShipyardConsoleComponent component, ComponentInit args)
        {
            if (!_enabled)
                return;

            SetupShipyard();
        }

        private void OnRoundRestart(RoundRestartCleanupEvent ev)
        {
            _configManager.UnsubValueChanged(CCVars.Shipyard, SetShipyardEnabled);
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
        public void PurchaseShuttle(EntityUid? stationUid, string shuttlePath, out ShuttleComponent? vessel)
        {
            if (!TryComp<StationDataComponent>(stationUid, out var stationData) || !TryComp<ShuttleComponent>(AddShuttle(shuttlePath), out var shuttle))
            {
                vessel = null;
                return;
            }

            var targetGrid = _station.GetLargestGrid(stationData);

            if (targetGrid == null)
            {
                vessel = null;
                return;
            }

            var price = _pricing.AppraiseGrid(shuttle.Owner, null);

            //can do TryFTLDock later instead if we need to keep the shipyard map paused
            _shuttle.FTLTravel(shuttle, targetGrid.Value, 0f, 30f, true);
            vessel = shuttle;
            _sawmill.Info($"Shuttle {shuttlePath} was purchased at {targetGrid} for {price}");
        }

        /// <summary>
        /// Loads a shuttle into the ShipyardMap from a file path
        /// </summary>
        /// <param name="shuttlePath">The path to the grid file to load. Must be a grid file!</param>
        /// <returns>Returns the EntityUid of the shuttle</returns>
        private EntityUid? AddShuttle(string shuttlePath)
        {
            if (ShipyardMap == null)
                return null;

            var loadOptions = new MapLoadOptions()
            {
                Offset = (500f + _shuttleIndex, 0f)
            };

            if (!_map.TryLoad(ShipyardMap.Value, shuttlePath.ToString(), out var gridList, loadOptions) || gridList == null)
            {
                _sawmill.Error($"Unable to spawn shuttle {shuttlePath}");
                return null;
            };

            _shuttleIndex += _mapManager.GetGrid(gridList[0]).LocalAABB.Width + ShuttleSpawnBuffer;
            var actualGrids = new List<EntityUid>();
            var gridQuery = GetEntityQuery<MapGridComponent>();

            foreach (var ent in gridList)
            {
                if (!gridQuery.HasComponent(ent))
                    continue;

                actualGrids.Add(ent);
            };

            //only dealing with 1 grid at a time for now, until more is known about multi-grid drifting
            if (actualGrids.Count != 1)
            {
                _sawmill.Error($"Unable to spawn shuttle {shuttlePath}");
                if (actualGrids.Count > 1)
                {
                    foreach (var grid in actualGrids)
                    {
                        _mapManager.DeleteGrid(grid);
                    }
                }
                return null;
            };

            return actualGrids[0];
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
}
