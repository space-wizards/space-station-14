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
       EntityUid? largestGrid = null;
       Box2? largestBounds;
       Box2? shuttleBounds = null;
       var validSpawn = false;

       // TODO: Copy-paste with docking, shitcode
       if (TryComp<StationDataComponent>(stationUid, out var dataComponent) && dataComponent.EmergencyShuttle != null)
       {
           (largestGrid, largestBounds) = GetLargestGrid(dataComponent);

           if (largestGrid != null)
           {
               var largestGridGrid = Comp<IMapGridComponent>(largestGrid.Value);
               var (shuttleDockXform, shuttleDock) = GetShuttleDock(dataComponent.EmergencyShuttle.Value);
               var shuttleAABB = Comp<IMapGridComponent>(dataComponent.EmergencyShuttle.Value).Grid.LocalAABB;

               if (shuttleDockXform != null)
               {
                   // First, get the station dock's position relative to the shuttle, this is where we rotate it around
                   var stationDockPos = shuttleDockXform.LocalPosition +
                                        shuttleDockXform.LocalRotation.RotateVec(new Vector2(0f, -1f));

                   var stationDockMatrix = Matrix3.CreateInverseTransform(stationDockPos, shuttleDockXform.LocalRotation);

                   foreach (var (comp, xform) in EntityQuery<DockingComponent, TransformComponent>(true))
                   {
                       if (xform.ParentUid != largestGrid.Value || comp.Docked) continue;

                       // Now get the rotation difference between the 2 and rotate the shuttle position by that.
                       var xformMatrix = Matrix3.CreateTransform(xform.LocalPosition, -xform.LocalRotation);
                       Matrix3.Multiply(in stationDockMatrix, in xformMatrix, out var matty);
                       shuttleBounds = matty.TransformBox(shuttleAABB);

                       if (!ValidSpawn(largestGridGrid, shuttleBounds.Value)) continue;

                       validSpawn = true;
                       break;
                   }
                   // TODO: Get a list of valid dock spawns
                   // Then for each one work out how many docks we can combine
                   // From there prioritise the highest.

               }
           }
       }

       RaiseNetworkEvent(new EmergencyShuttlePositionMessage()
       {
            StationUid = validSpawn ? largestGrid : null,
            Position = validSpawn ? shuttleBounds : null,
       });
   }

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

       var (largestGrid, largestBounds) = GetLargestGrid(component);

       if (largestGrid != null)
       {
           var (shuttleDockXform, shuttleDock) = GetShuttleDock(largestGrid.Value);

           if (shuttleDockXform != null)
           {
               // First, get the station dock's position relative to the shuttle, this is where we rotate it around
               var stationDockPos = shuttleDockXform.LocalPosition +
                                    shuttleDockXform.LocalRotation.RotateVec(new Vector2(0f, 1f));

               var largestGridXform = Transform(largestGrid.Value);
               var largestGridMatrix = largestGridXform.WorldMatrix;

               foreach (var (comp, xform) in EntityQuery<DockingComponent, TransformComponent>(true))
               {
                   if (xform.ParentUid != largestGrid.Value || comp.Docked) continue;

                   // Now get the rotation difference between the 2 and rotate the shuttle position by that.
                   var rotation = (shuttleDockXform.LocalRotation - xform.LocalRotation);
                   var shuttlePos = xform.LocalPosition + rotation.RotateVec(stationDockPos);

                   // TODO: Check clearance
                   var shuttleBounds = largestGridMatrix.Transform(shuttlePos);
               }
           }
       }
       // TODO: Uhh fallback?
       else
       {

       }

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

   private (TransformComponent?, DockingComponent?) GetShuttleDock(EntityUid uid)
   {
       foreach (var (dock, xform) in EntityQuery<DockingComponent, TransformComponent>(true))
       {
           if (xform.ParentUid != uid || !dock.Enabled) continue;

           return (xform, dock);
       }

       return (null, null);
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
