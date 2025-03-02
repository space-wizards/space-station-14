using Content.Server.AlertLevel;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Coordinates;
using Content.Shared.DropPod;
using Content.Shared.Maps;
using Content.Shared.Weather;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using System.Threading;

namespace Content.Server.DropPod
{
    public sealed class DropPodSystem : EntitySystem
    {
        [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
        [Dependency] private readonly AlertLevelSystem _alertLevelSystem = default!;
        [Dependency] private readonly ChatSystem _chat = default!;
        [Dependency] private readonly EntityLookupSystem _lookupSystem = default!;
        [Dependency] private readonly IEntityManager _entMan = default!;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
        [Dependency] private readonly SharedMapSystem _mapSystem = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;
        [Dependency] private readonly StationSystem _station = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterface = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DropPodConsoleComponent, BoundUIOpenedEvent>(OnBoundUIOpened);
            SubscribeLocalEvent<DropPodConsoleComponent, DropPodRefreshMessage>(OnRefreshLandingButtonPressed);
            SubscribeLocalEvent<DropPodConsoleComponent, DropPodStartMessage>(OnStartLandingButtonPressed);
            SubscribeLocalEvent<DropPodConsoleComponent, DropPodPointSelectedMessage>(OnPointSelected);
        }

        private int UIN = 0;
        private bool canActivate = false;
        private float top_border = 0f;
        private float bottom_border = 0f;
        private float left_border = 0f;
        private float right_border = 0f;

        #region Ui
        private void OnPointSelected(EntityUid uid, DropPodConsoleComponent component, DropPodPointSelectedMessage args)
        {
            UIN = args.Point; // we remember the unique identification number of the point, for further work with it
            canActivate = true; // we give you the opportunity to start disembarking

            UpdateUI(uid);
        }

        private void OnBoundUIOpened(EntityUid uid, DropPodConsoleComponent component, BoundUIOpenedEvent args)
        {
            canActivate = false;

            UpdateUI(uid);
        }


        private void OnRefreshLandingButtonPressed(EntityUid uid, DropPodConsoleComponent component, DropPodRefreshMessage message)
        {
            canActivate = false;

            UpdateUI(uid);
        }

        private void UpdateUI(EntityUid uid)
        {
            Dictionary<int, string> points = new Dictionary<int, string>(); // A dictionary is needed to convey the names of points
            var query = AllEntityQuery<LandingPointComponent>();
            while (query.MoveNext(out var item_uid, out var item_comp))
            {
                points.Add(item_comp.UIN, item_comp.NameLandingPoint);
            }

            var state = new DropPodUiState(true, canActivate, points);
            _userInterface.SetUiState(uid, DropPodUiKey.Key, state); // Updating the user interface
        }

        private void OnStartLandingButtonPressed(EntityUid uid, DropPodConsoleComponent component, DropPodStartMessage message)
        {
            EntityUid end_station_uid = uid;
            EntityUid end_stationAlert_uid = uid;
            EntityUid start_station_uid = uid;
            var x_form = Transform(uid);
            var Coords = x_form.Coordinates;
            int count = 0;

            var query_point = AllEntityQuery<LandingPointComponent>();
            while (query_point.MoveNext(out var item_uid, out var item_comp))
            {
                if (item_comp.UIN == UIN)
                {
                    x_form = Transform(item_uid);
                    Coords = x_form.Coordinates;
                    var query_station = AllEntityQuery<BecomesStationComponent>();
                    while (query_station.MoveNext(out var st_uid, out var data))
                    {
                        if (Transform(st_uid).MapID == x_form.MapID)
                        {
                            end_station_uid = st_uid;
                            break;
                        }
                    }

                    var query_stationAlert = EntityQueryEnumerator<StationDataComponent>();
                    while (query_stationAlert.MoveNext(out var stA_uid, out var dataA))
                    {
                        foreach (var gridUid in dataA.Grids)
                        {
                            if (Transform(gridUid).MapID == x_form.MapID)
                            {
                                end_stationAlert_uid = stA_uid;
                                break;
                            }
                        }
                    }
                    break;
                }
            }

            var query_lighthouse = AllEntityQuery<DropPodBeaconComponent>();
            while (query_lighthouse.MoveNext(out var item_uid, out var item_comp))
            {
                x_form = Transform(item_uid);
                var landinglighthousCoords = x_form.Coordinates;
                if (count == 0) // We assign the values of the first "beacon"
                {
                    top_border = landinglighthousCoords.Y;
                    bottom_border = landinglighthousCoords.Y;
                    left_border = landinglighthousCoords.X;
                    right_border = landinglighthousCoords.X;
                    count++;
                    continue;
                }

                if (landinglighthousCoords.Y < bottom_border)
                    bottom_border = landinglighthousCoords.Y;
                if (landinglighthousCoords.Y > top_border)
                    top_border = landinglighthousCoords.Y;
                if (landinglighthousCoords.X > right_border)
                    right_border = landinglighthousCoords.X;
                if (landinglighthousCoords.X < left_border)
                    left_border = landinglighthousCoords.X;

                var query_station = AllEntityQuery<BecomesStationComponent>();
                while (query_station.MoveNext(out var st_uid, out var data))
                {
                    if (Transform(st_uid).MapID == x_form.MapID)
                    {
                        start_station_uid = st_uid;
                        break;
                    }
                }
            }

            if (!component.WarDeclared & component.Announcement) // If war has not been declared, then the landing will be loud
            {
                _alertLevelSystem.SetLevel(end_stationAlert_uid, "red", true, true, true);
                _chat.DispatchGlobalAnnouncement($"{component.Text} X: {Coords.X} Y: {Coords.Y}", "Central Command", true, component.Sound, component.Color);
                Thread.Sleep(component.Time * 1000);
            }

            if (!TryComp<MapGridComponent>(end_station_uid, out var end_station_gridComp))
                return;

            var xform = Transform(end_station_uid);
            Random rnd = new Random();
            while (true)
            {
                int radius = rnd.Next(-5, 6);
                var x_coord = (int) (Coords.X - 0.5f) + radius; // This way we create a spread on landing to make it more interesting (spread within 5 tiles).
                radius = rnd.Next(-5, 6);
                var y_coord = (int) (Coords.Y - 0.5f) + radius;

                var tile = new Vector2i(x_coord, y_coord);
                if (_atmosphere.IsTileSpace(end_station_uid, xform.MapUid, tile)) // the point from which the drop pod generation starts should not be a space tile...
                {
                    continue; // if this is the case, then we are looking for the next possible point
                }

                var pos = _mapSystem.GridTileToLocal(end_station_uid, end_station_gridComp, tile);
                if (!TryComp<MapGridComponent>(start_station_uid, out var start_station_gridComp))
                    return;

                CheckTils(start_station_uid, start_station_gridComp, end_station_uid, end_station_gridComp, pos);
                break;
            }
        }
        #endregion

        #region moving the capsule
        /// <summary>
        /// This method iterates through all tiles within our borders (which are set by beacons) and creates similar ones (objects are transferred, not created) at the point of grounding
        /// </summary>
        /// <param name="start_station_uid"> it is needed to identify an object that is located in a certain tile at the Nuke Ops station </param>
        /// <param name="start_grid"> it is needed to determine the type of tile at the Nuke Ops station </param>
        /// <param name="end_station_uid"> it is necessary to understand where to move objects </param>
        /// <param name="end_grid"> it is needed to determine the local coordinates at the station where the disembarkation will take place </param>
        /// <param name="pos"> it is necessary to move the object to the desired coordinates </param>
        private void CheckTils(EntityUid start_station_uid, MapGridComponent start_grid, EntityUid end_station_uid, MapGridComponent end_grid, EntityCoordinates pos)
        {
            left_border -= 0.5f;  // we are aligning the values of the borders, because now we are counting
            right_border -= 0.5f; // from the center of the tile (on which the lighthouse stands), and not from the lower-left corner
            top_border -= 0.5f;
            bottom_border -= 0.5f;

            for (int i = (int) left_border + 1; i < (int) right_border; i++)
            {
                for (int j = (int) top_border - 1; j > (int) bottom_border; j--)
                {
                    var coords = new Vector2i(i, j);

                    if (!start_grid.TryGetTileRef(coords, out var tileRef))
                        continue;

                    int dX = i - ((int) left_border + 1);
                    int dY = j - ((int) top_border - 1);
                    var TileType = tileRef.Tile.GetContentTileDefinition().ID; // defining the tile type
                    if (TileType == "Plating") // if it's just "plating", then the objects from this tile are not portable (they are outside the DropPod)
                    {
                        foreach (var entity in _lookupSystem.GetLocalEntitiesIntersecting(start_station_uid, coords))
                        {
                            if (TryComp<BlockWeatherComponent>(entity, out var BlockWeatherComp)) // We check for the presence of a wall above a tile of this type
                            {
                                CreateShuttleFloorUnderWall(end_station_uid, end_grid, pos, dX, dY);
                                if (_entMan.TryGetComponent(entity, out TransformComponent? transComp))
                                {
                                    if (transComp.Anchored)
                                    {
                                        CreateEntityOnShuttle(end_station_uid, end_grid, pos, dX, dY, entity, transComp, true, transComp.LocalRotation);
                                        continue;
                                    }
                                    CreateEntityOnShuttle(end_station_uid, end_grid, pos, dX, dY, entity, transComp, false, transComp.LocalRotation);
                                }
                            }
                        }
                        continue;
                    }

                    CreateShuttleFloor(end_station_uid, end_grid, pos, dX, dY, (string) TileType);
                    foreach (var entity in _lookupSystem.GetLocalEntitiesIntersecting(start_station_uid, coords))
                    {
                        if (_entMan.TryGetComponent(entity, out TransformComponent? transComp))
                        {
                            var parent = transComp.ParentUid;

                            if (parent == start_station_uid) // // this is necessary to teleport a man in a suit, not in parts :)
                            {
                                if (transComp.Anchored)
                                {
                                    CreateEntityOnShuttle(end_station_uid, end_grid, pos, dX, dY, entity, transComp, true, transComp.LocalRotation);
                                    continue;
                                }
                                CreateEntityOnShuttle(end_station_uid, end_grid, pos, dX, dY, entity, transComp, false, transComp.LocalRotation);
                            }
                        }
                    }
                    CreatePlatingFloor(start_station_uid, start_grid, i, j);
                }
            }
        }

        private void CreatePlatingFloor(EntityUid start_station_uid, MapGridComponent start_gridComp, int X, int Y)
        {
            var tile = new Vector2i(X, Y);
            var new_pos = _mapSystem.GridTileToLocal(start_station_uid, start_gridComp, tile);
            var plating = _tileDefinitionManager["Plating"];
            start_gridComp.SetTile(new_pos, new Tile(plating.TileId));
        }

        /// <summary>
        /// This method is needed to create a floor at the point of movement that corresponds to what is located at the Nuke Ops station
        /// </summary>
        private void CreateShuttleFloor(EntityUid end_station_uid, MapGridComponent end_gridComp, EntityCoordinates pos, int dX, int dY, string TileType)
        {
            int coordX = (int) (pos.X - 0.5f) + dX;
            int coordY = (int) (pos.Y - 0.5f) + dY;
            var tile = new Vector2i(coordX, coordY);
            foreach (var entity in _lookupSystem.GetLocalEntitiesIntersecting(end_station_uid, tile))
            {

                if (TryComp<LandingPointComponent>(entity, out var LandComp))
                    continue;

                Del(entity); // iterate over the objects that are at the desired coordinates and delete them
            }
            var new_pos = _mapSystem.GridTileToLocal(end_station_uid, end_gridComp, tile);
            var plating = _tileDefinitionManager[TileType];
            end_gridComp.SetTile(new_pos, new Tile(plating.TileId));
        }


        /// <summary>
        /// This method is needed to create a floor under the walls.
        /// If part of the shuttle ends up in space, then this method will create a beautiful and logical grid,
        /// rather than a steel floor under the wall (this is important when using corner walls)
        /// </summary>
        private void CreateShuttleFloorUnderWall(EntityUid end_station_uid, MapGridComponent end_gridComp, EntityCoordinates pos, int dX, int dY)
        {
            int coordX = (int) (pos.X - 0.5f) + dX;
            int coordY = (int) (pos.Y - 0.5f) + dY;
            var tile = new Vector2i(coordX, coordY);
            var xform = Transform(end_station_uid);
            var new_pos = _mapSystem.GridTileToLocal(end_station_uid, end_gridComp, tile);
            var plating = _tileDefinitionManager["Lattice"];

            if (_atmosphere.IsTileSpace(end_station_uid, xform.MapUid, tile))
            {
                end_gridComp.SetTile(new_pos, new Tile(plating.TileId));
            }

            foreach (var entity in _lookupSystem.GetLocalEntitiesIntersecting(end_station_uid, tile)) // iterate over the objects that are at the desired coordinates and delete them
            {
                if (TryComp<LandingPointComponent>(entity, out var LandComp))
                    continue;

                Del(entity);
            }
        }


        /// <summary>
        /// This method is needed to move objects from one station to another when the DropPod is activated.
        /// We just change the coordinates of the objects and save some of their components.
        /// </summary>
        private void CreateEntityOnShuttle(EntityUid end_station_uid, MapGridComponent end_gridComp, EntityCoordinates pos, int dX, int dY, EntityUid entity, TransformComponent transComp, bool isAnch, Angle rot)
        {
            int coordX = (int) (pos.X - 0.5f) + dX;
            int coordY = (int) (pos.Y - 0.5f) + dY;
            var tile = new Vector2i(coordX, coordY);
            var new_pos = _mapSystem.GridTileToLocal(end_station_uid, end_gridComp, tile);
            _transform.SetCoordinates(entity, new_pos);
            transComp.Anchored = isAnch; // objects (e.g. walls) lose their attachment to the floor during teleportation
            transComp.LocalRotation = rot; // this is necessary so that the object is correctly rotated relative to the new station 
        }
        #endregion
    }
}
