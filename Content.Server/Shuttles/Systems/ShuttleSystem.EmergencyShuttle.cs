using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Communications;
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
using Robust.Shared.Random;

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
   [Dependency] private readonly IRobustRandom _random = default!;
   [Dependency] private readonly ChatSystem _chatSystem = default!;
   [Dependency] private readonly CommunicationsConsoleSystem _commsConsole = default!;
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

   /// <summary>
   /// If the client is requesting debug info on where an emergency shuttle would dock.
   /// </summary>
   private void OnShuttleRequestPosition(EmergencyShuttleRequestPositionMessage msg, EntitySessionEventArgs args)
   {
       if (!_admin.IsAdmin((IPlayerSession) args.SenderSession)) return;

       var player = args.SenderSession.AttachedEntity;

       if (player == null ||
           !TryComp<StationDataComponent>(_station.GetOwningStation(player.Value), out var stationData)) return;

       var config = GetDockingConfig(stationData);

       if (config != null)
       {
           RaiseNetworkEvent(new EmergencyShuttlePositionMessage()
           {
               StationUid = config.TargetGrid,
               Position = config.Area,
           });
       }
   }

   /// <summary>
   /// Checks whether the emergency shuttle can warp to the specified position.
   /// </summary>
   private bool ValidSpawn(IMapGridComponent grid, Box2 area)
   {
       return !grid.Grid.GetLocalTilesIntersecting(area).Any();
   }

   /// <summary>
   /// Tries to find the most valid docking config for the station.
   /// </summary>
   private DockingConfig? GetDockingConfig(StationDataComponent dataComponent)
   {
       // Find the largest grid associated with the station, then try all combinations of docks on it with
       // all of them on the shuttle and try to find the most appropriate.
       if (dataComponent.EmergencyShuttle == null) return null;

       var targetGrid = GetLargestGrid(dataComponent);

       if (targetGrid == null) return null;
       var gridDocks = GetDocks(targetGrid.Value);

       if (gridDocks.Count <= 0) return null;

       var xformQuery = GetEntityQuery<TransformComponent>();
       var targetGridGrid = Comp<IMapGridComponent>(targetGrid.Value);
       var targetGridXform = xformQuery.GetComponent(targetGrid.Value);
       var targetGridRotation = targetGridXform.WorldRotation.ToVec();

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

                   if (!CanDock(
                           shuttleDock, shuttleDockXform,
                           gridDock, gridXform,
                           targetGridRotation,
                           shuttleAABB,
                           targetGridGrid,
                           out var dockedAABB,
                           out var matty,
                           out var targetAngle)) continue;

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
                                   targetGridRotation,
                                   shuttleAABB, targetGridGrid,
                                   out var otherDockedAABB,
                                   out _,
                                   out var otherTargetAngle) ||
                               !otherDockedAABB.Equals(dockedAABB) ||
                               !targetAngle.Equals(otherTargetAngle)) continue;

                           dockedPorts.Add((other, otherGrid));
                       }
                   }

                   var spawnPosition = new EntityCoordinates(targetGrid.Value, matty.Transform(Vector2.Zero));
                   spawnPosition = new EntityCoordinates(targetGridXform.MapUid!.Value, spawnPosition.ToMapPos(EntityManager));
                   var spawnRotation = shuttleDockXform.LocalRotation +
                                       gridXform.LocalRotation +
                                       targetGridXform.LocalRotation;

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

       if (validDockConfigs.Count <= 0) return null;

       var targetGridAngle = targetGridXform.WorldRotation.Reduced();

       // Prioritise by priority docks, then by maximum connected ports, then by most similar angle.
       validDockConfigs = validDockConfigs
           .OrderByDescending(x => x.Docks.Any(docks => HasComp<EmergencyDockComponent>(docks.DockB.Owner)))
           .ThenByDescending(x => x.Docks.Count)
           .ThenBy(x => Math.Abs(Angle.ShortestDistance(x.Angle.Reduced(), targetGridAngle).Theta)).ToList();

       var location = validDockConfigs.First();
       location.TargetGrid = targetGrid.Value;
       // TODO: Ideally do a hyperspace warpin, just have it run on like a 10 second timer.

       return location;
   }

   /// <summary>
   /// Calls the emergency shuttle for the station.
   /// </summary>
   /// <param name="stationUid"></param>
   /// <param name="dryRun">Should we show the debug data and not actually call it.</param>
   public void CallEmergencyShuttle(EntityUid? stationUid)
   {
       if (!TryComp<StationDataComponent>(stationUid, out var stationData) ||
           !TryComp<TransformComponent>(stationData.EmergencyShuttle, out var xform)) return;

       var config = GetDockingConfig(stationData);

       if (config != null)
       {
           // Set position
           xform.Coordinates = config.Coordinates;
           xform.WorldRotation = config.Angle;

           // Connect everything
           foreach (var (dockA, dockB) in config.Docks)
           {
               _dockSystem.Dock(dockA, dockB);
           }

           _logger.Add(LogType.EmergencyShuttle, LogImpact.High, $"Emergency shuttle {ToPrettyString(stationUid.Value)} docked with stations");
           _chatSystem.DispatchStationAnnouncement(stationUid.Value, Loc.GetString("emergency-shuttle-docked", ("time", $"{_consoleAccumulator:0}")), playDefaultSound: false);
           // TODO: Need filter extensions or something don't blame me.
           SoundSystem.Play("/Audio/Announcements/shuttle_dock.ogg", Filter.Broadcast());
       }
       else
       {
           var shuttleAABB = Comp<IMapGridComponent>(stationData.EmergencyShuttle.Value).Grid.WorldAABB;
           Box2? aabb = null;

           // Spawn nearby.
           foreach (var gridUid in stationData.Grids)
           {
               var grid = Comp<IMapGridComponent>(gridUid).Grid;
               var gridAABB = grid.WorldAABB;
               aabb = aabb?.Union(gridAABB) ?? gridAABB;
           }

           // UHH GOOD LUCK
           if (aabb == null)
           {
               _logger.Add(LogType.EmergencyShuttle, LogImpact.High, $"Emergency shuttle {ToPrettyString(stationUid.Value)} unable to dock with station {ToPrettyString(stationUid.Value)}");
               _chatSystem.DispatchStationAnnouncement(stationUid.Value, Loc.GetString("emergency-shuttle-good-luck"), playDefaultSound: false);
               // TODO: Need filter extensions or something don't blame me.
               SoundSystem.Play("/Audio/Misc/notice1.ogg", Filter.Broadcast());


               return;
           }

           var minRadius = MathF.Max(aabb.Value.Width, aabb.Value.Height) + MathF.Max(shuttleAABB.Width, shuttleAABB.Height);
           var spawnPos = aabb.Value.Center + _random.NextVector2(minRadius, minRadius + 10f);

           if (TryComp<PhysicsComponent>(stationData.EmergencyShuttle, out var shuttleBody))
           {
               shuttleBody.LinearVelocity = Vector2.Zero;
               shuttleBody.AngularVelocity = 0f;
           }

           xform.WorldPosition = spawnPos;
           xform.WorldRotation = _random.NextAngle();

           _logger.Add(LogType.EmergencyShuttle, LogImpact.High, $"Emergency shuttle {ToPrettyString(stationUid.Value)} unable to find a valid docking port for {ToPrettyString(stationUid.Value)}");
           _chatSystem.DispatchStationAnnouncement(stationUid.Value, Loc.GetString("emergency-shuttle-nearby"), playDefaultSound: false);
           // TODO: Need filter extensions or something don't blame me.
           SoundSystem.Play("/Audio/Misc/notice1.ogg", Filter.Broadcast());
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
       Vector2 targetGridRotation,
       Box2 shuttleAABB,
       IMapGridComponent grid,
       [NotNullWhen(true)] out Box2? shuttleDockedAABB,
       out Matrix3 matty,
       out Vector2 gridRotation)
   {
       gridRotation = Vector2.Zero;
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

       var stationDockMatrix = Matrix3.CreateInverseTransform(stationDockPos, -shuttleXform.LocalRotation);
       var gridXformMatrix = Matrix3.CreateTransform(gridXform.LocalPosition, gridXform.LocalRotation);
       Matrix3.Multiply(in stationDockMatrix, in gridXformMatrix, out matty);
       shuttleDockedAABB = matty.TransformBox(shuttleAABB);

       if (!ValidSpawn(grid, shuttleDockedAABB.Value)) return false;

       gridRotation = matty.Transform(targetGridRotation);
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
       if (EmergencyShuttleArrived) return;

       _consoleAccumulator = _configManager.GetCVar(CCVars.EmergencyShuttleDockTime);
       EmergencyShuttleArrived = true;

       if (_centcommMap != null)
         _mapManager.SetMapPaused(_centcommMap.Value, false);

       foreach (var comp in EntityQuery<StationDataComponent>(true))
       {
           CallEmergencyShuttle(comp.Owner);
       }

       _commsConsole.UpdateCommsConsoleInterface();
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

   private void CleanupEmergencyShuttle()
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

       /// <summary>
       /// Target grid for docking.
       /// </summary>
       public EntityUid TargetGrid;

       public EntityCoordinates Coordinates;
       public Angle Angle;
   }
}
