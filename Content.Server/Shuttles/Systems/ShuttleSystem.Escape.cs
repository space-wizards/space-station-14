using Content.Server.GameTicking.Events;
using Content.Server.Shuttles.Components;
using Content.Server.Station.Components;
using Robust.Server.Maps;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Server.Shuttles.Systems;

public sealed partial class ShuttleSystem
{
   /*
    * Handles the escape shuttle + Centcomm
    */

   [Dependency] private readonly IMapLoader _loader = default!;

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
   }

   private void OnStationStartup(EntityUid uid, StationDataComponent component, ComponentStartup args)
   {
       if (_centcommMap == null) return;

       // TODO: Support multiple stations dingus.

       // Load escape shuttle
       var (_, shuttle) = _loader.LoadBlueprint(_centcommMap.Value, "/Maps/cargo_shuttle.yml", new MapLoadOptions()
       {
           // Should be far enough... right? I'm too lazy to bounds check centcomm.
           Offset = Vector2.One * 500f,
       });

       component.EmergencyShuttle = shuttle;
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

       // TODO: Use a GetEntityQuery<T> for this.
       EntityUid? largestGrid = null;
       Box2 largestBounds = new Box2();

       foreach (var gridUid in component.Grids)
       {
           if (!TryComp<IMapGridComponent>(gridUid, out var grid)) continue;

           if (grid.Grid.LocalAABB.Size.LengthSquared < largestBounds.Size.LengthSquared) continue;

           largestBounds = grid.Grid.LocalAABB;
           largestGrid = gridUid;
       }

       if (largestGrid != null)
       {
           var (shuttleDockXform, shuttleDock) = GetShuttleDock(largestGrid.Value);

           if (shuttleDockXform != null)
           {
               // First, get the station dock's position relative to the shuttle, this is where we rotate it around
               var stationDockPos = shuttleDockXform.LocalPosition +
                                    shuttleDockXform.LocalRotation.RotateVec(new Vector2(0f, 1f));

               foreach (var (comp, xform) in EntityQuery<DockingComponent, TransformComponent>(true))
               {
                   if (xform.ParentUid != largestGrid.Value || comp.Docked) continue;

                   var rotation = (shuttleDockXform.LocalRotation - xform.LocalRotation);
                   var shuttlePos = xform.WorldPosition + rotation.RotateVec(stationDockPos);

                   // TODO: Check clearance
               }
           }
       }
       // TODO: Uhh fallback?
       else
       {

       }

       // TODO: Hyperspace arrival and squimsh anything in the way
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
   }

   private void CleanupEscape()
   {
       if (_centcommMap == null) return;

       _mapManager.DeleteMap(_centcommMap.Value);
   }
}
