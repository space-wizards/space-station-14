using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking.Events;
using Content.Server.Shuttles.Components;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Shuttles.Events;
using Robust.Server.Maps;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server.Shuttles.Systems;

public sealed partial class ShuttleSystem
{
   /*
    * Handles the escape shuttle + Centcomm.
    */

   [Dependency] private readonly IAdminLogManager _logger = default!;
   [Dependency] private readonly IAdminManager _admin = default!;
   [Dependency] private readonly IConfigurationManager _configManager = default!;
   [Dependency] private readonly IMapLoader _loader = default!;
   [Dependency] private readonly ChatSystem _chatSystem = default!;
   [Dependency] private readonly DockingSystem _dockSystem = default!;
   [Dependency] private readonly StationSystem _station = default!;

   private MapId? _centcommMap;

   /// <summary>
   /// Used for multiple shuttle spawn offsets.
   /// </summary>
   private float _shuttleIndex;

   private const float ShuttleSpawnBuffer = 1f;

   private void InitializeEscape()
   {
       SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
       SubscribeLocalEvent<StationDataComponent, ComponentStartup>(OnStationStartup);
       SubscribeNetworkEvent<EmergencyShuttleRequestPositionMessage>(OnShuttleRequestPosition);
   }

   private void OnShuttleRequestPosition(EmergencyShuttleRequestPositionMessage msg, EntitySessionEventArgs args)
   {
       if (!_admin.IsAdmin((IPlayerSession) args.SenderSession)) return;

       var player = args.SenderSession.AttachedEntity;

       if (player == null) return;

       var stationUid = _station.GetOwningStation(player.Value);
       CallEmergencyShuttle(stationUid, true);
   }

   /// <summary>
   /// Checks whether the emergency shuttle can warp to the specified position.
   /// </summary>
   private bool ValidSpawn(IMapGridComponent grid, Box2 area)
   {
       return !grid.Grid.GetLocalTilesIntersecting(area).Any();
   }

   /// <summary>
   /// Calls the emergency shuttle for the station.
   /// </summary>
   /// <param name="stationUid"></param>
   /// <param name="dryRun">Should we show the debug data and not actually call it.</param>
   public void CallEmergencyShuttle(EntityUid? stationUid, bool dryRun = false)
   {
       EntityUid? targetGrid = null;
       Box2? position = null;

       // Find the largest grid associated with the station, then try all combinations of docks on it with
       // all of them on the shuttle and try to find the most appropriate.
       if (TryComp<StationDataComponent>(stationUid, out var dataComponent) && dataComponent.EmergencyShuttle != null)
       {
           targetGrid = GetLargestGrid(dataComponent);

           if (targetGrid != null)
           {
               var gridDocks = GetDocks(targetGrid.Value);

               if (gridDocks.Count > 0)
               {
                   var xformQuery = GetEntityQuery<TransformComponent>();
                   var shuttleXform = xformQuery.GetComponent(dataComponent.EmergencyShuttle.Value);
                   var targetGridGrid = Comp<IMapGridComponent>(targetGrid.Value);
                   var targetGridXform = xformQuery.GetComponent(targetGrid.Value);
                   var shuttleDocks = GetDocks(dataComponent.EmergencyShuttle.Value);
                   var shuttleAABB = Comp<IMapGridComponent>(dataComponent.EmergencyShuttle.Value).Grid.LocalAABB;
                   var validDockConfigs = new List<DockingConfig>();

                   if (TryComp<ShuttleComponent>(dataComponent.EmergencyShuttle, out var shuttle))
                   {
                       SetPilotable(shuttle, false);
                   }

                   if (shuttleDocks.Count > 0)
                   {
                       // We'll try all combinations of shuttle docks and see which one is most suitable
                       foreach (var shuttleDock in shuttleDocks)
                       {
                           var shuttleDockXform = xformQuery.GetComponent(shuttleDock.Owner);

                           foreach (var gridDock in gridDocks)
                           {
                               var gridXform = xformQuery.GetComponent(gridDock.Owner);

                               if (!CanDock(shuttleDock, shuttleDockXform, gridDock, gridXform, shuttleAABB, targetGridGrid, out var dockedAABB, out var matty)) continue;

                               // Alright well the spawn is valid now to check how many we can connect
                               // Get the matrix for each shuttle dock and test it against the grid docks to see
                               // if the connected position / direction matches.

                               var dockedPorts = new List<(DockingComponent DockA, DockingComponent DockB)>()
                               {
                                   (shuttleDock, gridDock),
                               };

                               // TODO: Check shuttle orientation as the tiebreaker.

                               foreach (var other in shuttleDocks)
                               {
                                   if (other == shuttleDock) continue;

                                   foreach (var otherGrid in gridDocks)
                                   {
                                       if (otherGrid == gridDock) continue;

                                       if (!CanDock(
                                               other,
                                               xformQuery.GetComponent(other.Owner),
                                               otherGrid,
                                               xformQuery.GetComponent(otherGrid.Owner),
                                               shuttleAABB, targetGridGrid,
                                               out var otherDockedAABB,
                                               out _) ||
                                           !otherDockedAABB.Equals(dockedAABB)) continue;

                                       dockedPorts.Add((other, otherGrid));
                                   }
                               }

                               var spawnPosition = new EntityCoordinates(targetGrid.Value, matty.Transform(Vector2.Zero));
                               spawnPosition = new EntityCoordinates(targetGridXform.MapUid!.Value, spawnPosition.ToMapPos(EntityManager));
                               var spawnRotation = shuttleDockXform.LocalRotation +
                                   targetGridXform.LocalRotation +
                                   gridXform.LocalRotation;

                               validDockConfigs.Add(new DockingConfig()
                               {
                                   Docks = dockedPorts,
                                   Area = dockedAABB.Value,
                                   Coordinates = spawnPosition,
                                   Angle = spawnRotation,
                               });
                           }
                       }
                   }

                   if (validDockConfigs.Count > 0)
                   {
                       var targetGridAngle = targetGridXform.WorldRotation.Reduced();

                       // Prioritise maximum connected ports, then by most similar angle.
                       validDockConfigs = validDockConfigs
                           .OrderByDescending(x => x.Docks.Count)
                           .ThenByDescending(x => Angle.ShortestDistance(x.Angle.Reduced(), targetGridAngle).Theta).ToList();

                       var location = validDockConfigs.First();
                       position = location.Area;
                       // TODO: Ideally do a hyperspace warpin, just have it run on like a 10 second timer.

                       if (!dryRun)
                       {
                           // Set position
                           shuttleXform.Coordinates = location.Coordinates;
                           shuttleXform.WorldRotation = location.Angle;

                           // Connect everything
                           foreach (var (dockA, dockB) in location.Docks)
                           {
                               _dockSystem.Dock(dockA, dockB);
                           }
                       }
                   }
               }
           }
       }

       // TODO: Move to its own method
       if (dryRun)
       {
           RaiseNetworkEvent(new EmergencyShuttlePositionMessage()
           {
               StationUid = targetGrid,
               Position = position,
           });
       }
   }

   /// <summary>
   /// Checks if 2 docks can be connected by moving the shuttle directly onto docks.
   /// </summary>
   private bool CanDock(
       DockingComponent shuttleDock,
       TransformComponent shuttleXform,
       DockingComponent gridDock,
       TransformComponent gridXform,
       Box2 shuttleAABB,
       IMapGridComponent grid,
       [NotNullWhen(true)] out Box2? shuttleDockedAABB,
       out Matrix3 matty)
   {
       matty = Matrix3.Identity;
       shuttleDockedAABB = null;

       if (shuttleDock.Docked ||
           gridDock.Docked ||
           !shuttleXform.Anchored ||
           !gridXform.Anchored)
       {
           return false;
       }

       // First, get the station dock's position relative to the shuttle, this is where we rotate it around
       var stationDockPos = shuttleXform.LocalPosition +
                            shuttleXform.LocalRotation.RotateVec(new Vector2(0f, -1f));

       var stationDockMatrix = Matrix3.CreateInverseTransform(stationDockPos, shuttleXform.LocalRotation);
       var gridXformMatrix = Matrix3.CreateTransform(gridXform.LocalPosition, -gridXform.LocalRotation);
       Matrix3.Multiply(in stationDockMatrix, in gridXformMatrix, out matty);
       shuttleDockedAABB = matty.TransformBox(shuttleAABB);

       if (!ValidSpawn(grid, shuttleDockedAABB.Value)) return false;

       return true;
   }

   private void OnStationStartup(EntityUid uid, StationDataComponent component, ComponentStartup args)
   {
       AddEmergencyShuttle(component);
   }

   private void OnRoundStart(RoundStartingEvent ev)
   {
       Setup();
   }

   /// <summary>
   /// Spawns the emergency shuttle for each station and starts the countdown until controls unlock.
   /// </summary>
   public void CallEmergencyShuttle()
   {
       _logger.Add(LogType.EmergencyShuttle, LogImpact.High, $"Emergency shuttle docked with stations");
       SoundSystem.Play("/Audio/Announcements/shuttle_dock.ogg", Filter.Broadcast());

       foreach (var comp in EntityQuery<StationDataComponent>(true))
       {
           CallEmergencyShuttle(comp.Owner);
       }

       _chatSystem.DispatchGlobalStationAnnouncement($"The Emergency Shuttle has docked with the station", playDefaultSound: false);
       _consoleAccumulator = _configManager.GetCVar(CCVars.EmergencyShuttleDockTime);
   }

   /// <summary>
   /// Gets the largest member grid from a station.
   /// </summary>
   private EntityUid? GetLargestGrid(StationDataComponent component)
   {
       EntityUid? largestGrid = null;
       Box2 largestBounds = new Box2();

       foreach (var gridUid in component.Grids)
       {
           if (!TryComp<IMapGridComponent>(gridUid, out var grid)) continue;

           if (grid.Grid.LocalAABB.Size.LengthSquared < largestBounds.Size.LengthSquared) continue;

           largestBounds = grid.Grid.LocalAABB;
           largestGrid = gridUid;
       }

       return largestGrid;
   }

   private List<DockingComponent> GetDocks(EntityUid uid)
   {
       var result = new List<DockingComponent>();

       foreach (var (dock, xform) in EntityQuery<DockingComponent, TransformComponent>(true))
       {
           if (xform.ParentUid != uid || !dock.Enabled) continue;

           result.Add(dock);
       }

       return result;
   }

   private void Setup()
   {
       if (_centcommMap != null && _mapManager.MapExists(_centcommMap.Value)) return;

       _centcommMap = _mapManager.CreateMap();
       _mapManager.SetMapPaused(_centcommMap.Value, true);

       // Load Centcomm, when we get it!
       // var (_, centcomm) = _loader.LoadBlueprint(_centcommMap.Value, "/Maps/Salvage/saltern.yml", new MapLoadOptions());
       // _centcomm = centcomm;

       foreach (var comp in EntityQuery<StationDataComponent>(true))
       {
           AddEmergencyShuttle(comp);
       }
   }

   private void AddEmergencyShuttle(StationDataComponent component)
   {
       if (_centcommMap == null || component.EmergencyShuttle != null) return;

       // Load escape shuttle
       var (_, shuttle) = _loader.LoadBlueprint(_centcommMap.Value, component.EmergencyShuttlePath.ToString(), new MapLoadOptions()
       {
           // Should be far enough... right? I'm too lazy to bounds check centcomm rn.
           Offset = new Vector2(500f + _shuttleIndex, 0f)
       });

       if (shuttle == null)
       {
           _sawmill.Error($"Unable to spawn emergency shuttle {component.EmergencyShuttlePath} for {ToPrettyString(component.Owner)}");
           return;
       }

       _shuttleIndex += _mapManager.GetGrid(shuttle.Value).LocalAABB.Width + ShuttleSpawnBuffer;
       component.EmergencyShuttle = shuttle;
   }

   private void CleanupEscape()
   {
       _shuttleIndex = 0f;

       if (_centcommMap == null || !_mapManager.MapExists(_centcommMap.Value))
       {
           _centcommMap = null;
           return;
       }

       _mapManager.DeleteMap(_centcommMap.Value);
   }

   /// <summary>
   /// Stores the data for a valid docking configuration for the emergency shuttle
   /// </summary>
   private sealed class DockingConfig
   {
       /// <summary>
       /// The pairs of docks that can connect.
       /// </summary>
       public List<(DockingComponent DockA, DockingComponent DockB)> Docks = new();

       /// <summary>
       /// Area relative to the target grid the emergency shuttle will spawn in on.
       /// </summary>
       public Box2 Area;

       public EntityCoordinates Coordinates;

       public Angle Angle;
   }
}
