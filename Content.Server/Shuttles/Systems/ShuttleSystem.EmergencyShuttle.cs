using System.Linq;
using Content.Server.Administration.Managers;
using Content.Server.GameTicking.Events;
using Content.Server.Shuttles.Components;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Shuttles.Events;
using Robust.Server.Maps;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Server.Shuttles.Systems;

public sealed partial class ShuttleSystem
{
   /*
    * Handles the escape shuttle + Centcomm
    */

   [Dependency] private readonly IAdminManager _admin = default!;
   [Dependency] private readonly IMapLoader _loader = default!;
   [Dependency] private readonly StationSystem _station = default!;

   private MapId? _centcommMap;
   private EntityUid? _centcomm;

   // TODO: Use uhhhhhhhhh prototypes I guess?

   /*
    * TODO: When shuttle call < 30 seconds block recalls
    * TODO: When call happened issue event, that's when you start queueing hyperspace from Centcomm and activate Centcomm
    * TODO: After n time unlock all controls, maybe put it on the shuttle state? Ask slorkitos
    */

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
       EntityUid? targetGrid = null;
       Box2? position = null;

       // TODO: Copy-paste with docking, shitcode
       // Find the largest grid associated with the station, then try all combinations of docks on it with
       // all of them on the shuttle and try to find the most appropriate.
       if (TryComp<StationDataComponent>(stationUid, out var dataComponent) && dataComponent.EmergencyShuttle != null)
       {
           (targetGrid, _) = GetLargestGrid(dataComponent);

           if (targetGrid != null)
           {
               var gridDocks = GetDocks(targetGrid.Value);

               if (gridDocks.Count > 0)
               {
                   var targetGridGrid = Comp<IMapGridComponent>(targetGrid.Value);
                   var shuttleDocks = GetDocks(dataComponent.EmergencyShuttle.Value);
                   var shuttleAABB = Comp<IMapGridComponent>(dataComponent.EmergencyShuttle.Value).Grid.LocalAABB;
                   var validDockConfigs = new List<(DockingComponent Dock, int Count, Box2 Area)>();

                   if (shuttleDocks.Count > 0)
                   {
                       // We'll try all combinations of shuttle docks and see which one is most suitable
                       foreach (var dock in shuttleDocks)
                       {
                           if (dock.Docked) continue;

                           var shuttleDockXform = Transform(dock.Owner);

                           // First, get the station dock's position relative to the shuttle, this is where we rotate it around
                           var stationDockPos = shuttleDockXform.LocalPosition +
                                                shuttleDockXform.LocalRotation.RotateVec(new Vector2(0f, -1f));

                           var stationDockMatrix = Matrix3.CreateInverseTransform(stationDockPos, shuttleDockXform.LocalRotation);

                           foreach (var gridDock in gridDocks)
                           {
                               if (gridDock.Docked) continue;

                               var xform = Transform(gridDock.Owner);

                               // See if the spawn for the shuttle is valid
                               // Then we'll see what other dock spawns are valid.
                               var xformMatrix = Matrix3.CreateTransform(xform.LocalPosition, -xform.LocalRotation);
                               Matrix3.Multiply(in stationDockMatrix, in xformMatrix, out var matty);
                               Box2? shuttleBounds = matty.TransformBox(shuttleAABB);

                               if (!ValidSpawn(targetGridGrid, shuttleBounds.Value)) continue;

                               // Alright well the spawn is valid now to check how many we can connect
                               // Get the matrix for each shuttle dock and test it against the grid docks to see
                               // if the connected position / direction matches.
                               var connections = 1;

                               foreach (var other in shuttleDocks)
                               {
                                   if (dock == other || other.Docked) continue;
                                   // TODO: GetEntityQuery<TransformComponent>

                                   // TODO: Do matrix transformations to check configs
                                   foreach (var otherGrid in gridDocks)
                                   {
                                       if (otherGrid == gridDock || otherGrid.Docked) continue;
                                   }
                               }

                               validDockConfigs.Add((dock, connections, shuttleBounds.Value));
                           }
                       }
                   }

                   if (validDockConfigs.Count > 0)
                   {
                       var location = validDockConfigs.First();
                       position = location.Area;
                   }
               }
           }
       }

       RaiseNetworkEvent(new EmergencyShuttlePositionMessage()
       {
            StationUid = targetGrid,
            Position = position,
       });
   }

   /// <summary>
   /// Checks whether the emergency shuttle can warp to the specified position.
   /// </summary>
   private bool ValidSpawn(IMapGridComponent grid, Box2 area)
   {
       return !grid.Grid.GetLocalTilesIntersecting(area).Any();
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
   /// Spawns the escape shuttle for each station and starts the countdown until controls unlock.
   /// </summary>
   public void CallEscapeShuttle()
   {
       foreach (var comp in EntityQuery<StationDataComponent>())
       {
           DockEmergencyShuttle(comp);
       }

       // TODO: Set a timer for 4 minutes to launch

       // TODO: When EmergencyConsole triggered set to

       // TODO: When shuttle launches set a timer for round end.
   }

   private void DockEmergencyShuttle(StationDataComponent component)
   {
       // TODO: Dock it with the largest grid I guess?
       if (component.EmergencyShuttle == null) return;

       // TODO: Hyperspace arrival and squimsh anything in the way
   }

   private (EntityUid?, Box2?) GetLargestGrid(StationDataComponent component)
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

       return (largestGrid, largestBounds);
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
       DebugTools.Assert(_centcommMap == null);
       _centcommMap = _mapManager.CreateMap();
       _mapManager.SetMapPaused(_centcommMap.Value, true);

       // Load Centcomm
       var (_, centcomm) = _loader.LoadBlueprint(_centcommMap.Value, "/Maps/Salvage/stationstation.yml", new MapLoadOptions());
       _centcomm = centcomm;

       foreach (var comp in EntityQuery<StationDataComponent>(true))
       {
           AddEmergencyShuttle(comp);
       }
   }

   private void AddEmergencyShuttle(StationDataComponent component)
   {
       if (_centcommMap == null || component.EmergencyShuttle != null) return;

       // TODO: Support multiple stations dingus.

       // Load escape shuttle
       var (_, shuttle) = _loader.LoadBlueprint(_centcommMap.Value, "/Maps/cargo_shuttle.yml", new MapLoadOptions()
       {
           // Should be far enough... right? I'm too lazy to bounds check centcomm.
           Offset = Vector2.One * 500f,
       });

       component.EmergencyShuttle = shuttle;
   }

   private void CleanupEscape()
   {
       if (_centcommMap == null) return;

       _mapManager.DeleteMap(_centcommMap.Value);
   }
}
