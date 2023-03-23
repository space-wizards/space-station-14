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
using Content.Shared.Tiles;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Shuttles.Systems;

public sealed partial class ShuttleSystem
{
   /*
    * Handles the escape shuttle + CentCom.
    */

   [Dependency] private readonly IAdminLogManager _logger = default!;
   [Dependency] private readonly IAdminManager _admin = default!;
   [Dependency] private readonly IConfigurationManager _configManager = default!;
   [Dependency] private readonly IRobustRandom _random = default!;
   [Dependency] private readonly ChatSystem _chatSystem = default!;
   [Dependency] private readonly CommunicationsConsoleSystem _commsConsole = default!;
   [Dependency] private readonly DockingSystem _dockSystem = default!;
   [Dependency] private readonly MapLoaderSystem _map = default!;
   [Dependency] private readonly StationSystem _station = default!;

   public MapId? CentComMap { get; private set; }
   public EntityUid? CentCom { get; private set; }

   /// <summary>
   /// Used for multiple shuttle spawn offsets.
   /// </summary>
   private float _shuttleIndex;

   private const float ShuttleSpawnBuffer = 1f;

   private bool _emergencyShuttleEnabled;

   private void InitializeEscape()
   {
       _emergencyShuttleEnabled = _configManager.GetCVar(CCVars.EmergencyShuttleEnabled);
       // Don't immediately invoke as roundstart will just handle it.
       _configManager.OnValueChanged(CCVars.EmergencyShuttleEnabled, SetEmergencyShuttleEnabled);
       SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
       SubscribeLocalEvent<StationDataComponent, ComponentStartup>(OnStationStartup);
       SubscribeNetworkEvent<EmergencyShuttleRequestPositionMessage>(OnShuttleRequestPosition);
   }

   private void SetEmergencyShuttleEnabled(bool value)
   {
       if (_emergencyShuttleEnabled == value) return;
       _emergencyShuttleEnabled = value;

       if (value)
       {
           SetupEmergencyShuttle();
       }
       else
       {
           CleanupEmergencyShuttle();
       }
   }

   private void ShutdownEscape()
   {
        _configManager.UnsubValueChanged(CCVars.EmergencyShuttleEnabled, SetEmergencyShuttleEnabled);
   }

   /// <summary>
   /// If the client is requesting debug info on where an emergency shuttle would dock.
   /// </summary>
   private void OnShuttleRequestPosition(EmergencyShuttleRequestPositionMessage msg, EntitySessionEventArgs args)
   {
       if (!_admin.IsAdmin((IPlayerSession) args.SenderSession)) return;

       var player = args.SenderSession.AttachedEntity;

       if (player == null ||
           !TryComp<StationDataComponent>(_station.GetOwningStation(player.Value), out var stationData) ||
           !TryComp<ShuttleComponent>(stationData.EmergencyShuttle, out var shuttle)) return;

       var targetGrid = _station.GetLargestGrid(stationData);
       if (targetGrid == null) return;
       var config = GetDockingConfig(shuttle, targetGrid.Value);
       if (config == null) return;

       RaiseNetworkEvent(new EmergencyShuttlePositionMessage()
       {
           StationUid = targetGrid,
           Position = config.Area,
       });
   }

   /// <summary>
   /// Calls the emergency shuttle for the station.
   /// </summary>
   public void CallEmergencyShuttle(EntityUid? stationUid)
   {
       if (!TryComp<StationDataComponent>(stationUid, out var stationData) ||
           !TryComp<TransformComponent>(stationData.EmergencyShuttle, out var xform) ||
           !TryComp<ShuttleComponent>(stationData.EmergencyShuttle, out var shuttle)) return;

      var targetGrid = _station.GetLargestGrid(stationData);

       // UHH GOOD LUCK
       if (targetGrid == null)
       {
           _logger.Add(LogType.EmergencyShuttle, LogImpact.High, $"Emergency shuttle {ToPrettyString(stationUid.Value)} unable to dock with station {ToPrettyString(stationUid.Value)}");
           _chatSystem.DispatchStationAnnouncement(stationUid.Value, Loc.GetString("emergency-shuttle-good-luck"), playDefaultSound: false);
           // TODO: Need filter extensions or something don't blame me.
           SoundSystem.Play("/Audio/Misc/notice1.ogg", Filter.Broadcast());
           return;
       }

       var xformQuery = GetEntityQuery<TransformComponent>();

       if (TryFTLDock(shuttle, targetGrid.Value))
       {
           if (TryComp<TransformComponent>(targetGrid.Value, out var targetXform))
           {
               var angle = GetAngle(xform, targetXform, xformQuery);
               _chatSystem.DispatchStationAnnouncement(stationUid.Value, Loc.GetString("emergency-shuttle-docked", ("time", $"{_consoleAccumulator:0}"), ("direction", angle.GetDir())), playDefaultSound: false);
           }

           _logger.Add(LogType.EmergencyShuttle, LogImpact.High, $"Emergency shuttle {ToPrettyString(stationUid.Value)} docked with stations");
           // TODO: Need filter extensions or something don't blame me.
           SoundSystem.Play("/Audio/Announcements/shuttle_dock.ogg", Filter.Broadcast());
       }
       else
       {
           if (TryComp<TransformComponent>(targetGrid.Value, out var targetXform))
           {
               var angle = GetAngle(xform, targetXform, xformQuery);
               _chatSystem.DispatchStationAnnouncement(stationUid.Value, Loc.GetString("emergency-shuttle-nearby", ("direction", angle.GetDir())), playDefaultSound: false);
           }

           _logger.Add(LogType.EmergencyShuttle, LogImpact.High, $"Emergency shuttle {ToPrettyString(stationUid.Value)} unable to find a valid docking port for {ToPrettyString(stationUid.Value)}");
           // TODO: Need filter extensions or something don't blame me.
           SoundSystem.Play("/Audio/Misc/notice1.ogg", Filter.Broadcast());
       }
   }

   private Angle GetAngle(TransformComponent xform, TransformComponent targetXform, EntityQuery<TransformComponent> xformQuery)
   {
       var (shuttlePos, shuttleRot) = xform.GetWorldPositionRotation(xformQuery);
       var (targetPos, targetRot) = targetXform.GetWorldPositionRotation(xformQuery);

       var shuttleCOM = Robust.Shared.Physics.Transform.Mul(new Transform(shuttlePos, shuttleRot),
           Comp<PhysicsComponent>(xform.Owner).LocalCenter);
       var targetCOM = Robust.Shared.Physics.Transform.Mul(new Transform(targetPos, targetRot),
           Comp<PhysicsComponent>(targetXform.Owner).LocalCenter);

       var mapDiff = shuttleCOM - targetCOM;
       var targetRotation = targetRot;
       var angle = mapDiff.ToWorldAngle();
       angle -= targetRotation;
       return angle;
   }

   /// <summary>
   /// Checks if 2 docks can be connected by moving the shuttle directly onto docks.
   /// </summary>
   private bool CanDock(
       DockingComponent shuttleDock,
       TransformComponent shuttleDockXform,
       DockingComponent gridDock,
       TransformComponent gridDockXform,
       Angle targetGridRotation,
       Box2 shuttleAABB,
       EntityUid gridUid,
       MapGridComponent grid,
       [NotNullWhen(true)] out Box2? shuttleDockedAABB,
       out Matrix3 matty,
       out Angle gridRotation)
   {
       gridRotation = Angle.Zero;
       matty = Matrix3.Identity;
       shuttleDockedAABB = null;

       if (shuttleDock.Docked ||
           gridDock.Docked ||
           !shuttleDockXform.Anchored ||
           !gridDockXform.Anchored)
       {
           return false;
       }

       // First, get the station dock's position relative to the shuttle, this is where we rotate it around
       var stationDockPos = shuttleDockXform.LocalPosition +
                            shuttleDockXform.LocalRotation.RotateVec(new Vector2(0f, -1f));

       // Need to invert the grid's angle.
       var shuttleDockAngle = shuttleDockXform.LocalRotation;
       var gridDockAngle = gridDockXform.LocalRotation.Opposite();

       var stationDockMatrix = Matrix3.CreateInverseTransform(stationDockPos, shuttleDockAngle);
       var gridXformMatrix = Matrix3.CreateTransform(gridDockXform.LocalPosition, gridDockAngle);
       Matrix3.Multiply(in stationDockMatrix, in gridXformMatrix, out matty);
       shuttleDockedAABB = matty.TransformBox(shuttleAABB);
       // Rounding moment
       shuttleDockedAABB = shuttleDockedAABB.Value.Enlarged(-0.01f);

       if (!ValidSpawn(gridUid, grid, shuttleDockedAABB.Value))
           return false;

       gridRotation = targetGridRotation + gridDockAngle - shuttleDockAngle;
       return true;
   }

   private void OnStationStartup(EntityUid uid, StationDataComponent component, ComponentStartup args)
   {
       AddEmergencyShuttle(component);
   }

   private void OnRoundStart(RoundStartingEvent ev)
   {
       SetupEmergencyShuttle();
   }

   /// <summary>
   /// Spawns the emergency shuttle for each station and starts the countdown until controls unlock.
   /// </summary>
   public void CallEmergencyShuttle()
   {
       if (EmergencyShuttleArrived) return;

       if (!_emergencyShuttleEnabled)
       {
           _roundEnd.EndRound();
           return;
       }

       _consoleAccumulator = _configManager.GetCVar(CCVars.EmergencyShuttleDockTime);
       EmergencyShuttleArrived = true;

       if (CentComMap != null)
         _mapManager.SetMapPaused(CentComMap.Value, false);

       foreach (var comp in EntityQuery<StationDataComponent>(true))
       {
           CallEmergencyShuttle(comp.Owner);
       }

       _commsConsole.UpdateCommsConsoleInterface();
   }

   public List<DockingComponent> GetDocks(EntityUid uid)
   {
       var result = new List<DockingComponent>();

       foreach (var (dock, xform) in EntityQuery<DockingComponent, TransformComponent>(true))
       {
           if (xform.ParentUid != uid || !dock.Enabled) continue;

           result.Add(dock);
       }

       return result;
   }

   private void SetupEmergencyShuttle()
   {
       if (!_emergencyShuttleEnabled || CentComMap != null && _mapManager.MapExists(CentComMap.Value)) return;

       CentComMap = _mapManager.CreateMap();
       _mapManager.SetMapPaused(CentComMap.Value, true);

       // Load CentCom
       var centComPath = _configManager.GetCVar(CCVars.CentcommMap);

       if (!string.IsNullOrEmpty(centComPath))
       {
           var centcomm = _map.LoadGrid(CentComMap.Value, "/Maps/centcomm.yml");
           CentCom = centcomm;

           if (CentCom != null)
               AddFTLDestination(CentCom.Value, false);
       }
       else
       {
           _sawmill.Info("No CentCom map found, skipping setup.");
       }

       foreach (var comp in EntityQuery<StationDataComponent>(true))
       {
           AddEmergencyShuttle(comp);
       }
   }

   private void AddEmergencyShuttle(StationDataComponent component)
   {
       if (!_emergencyShuttleEnabled
           || CentComMap == null
           || component.EmergencyShuttle != null
           || component.StationConfig == null)
       {
           return;
       }

       // Load escape shuttle
       var shuttlePath = component.StationConfig.EmergencyShuttlePath;
       var shuttle = _map.LoadGrid(CentComMap.Value, shuttlePath.ToString(), new MapLoadOptions()
       {
           // Should be far enough... right? I'm too lazy to bounds check CentCom rn.
           Offset = new Vector2(500f + _shuttleIndex, 0f)
       });

       if (shuttle == null)
       {
           _sawmill.Error($"Unable to spawn emergency shuttle {shuttlePath} for {ToPrettyString(component.Owner)}");
           return;
       }

       _shuttleIndex += _mapManager.GetGrid(shuttle.Value).LocalAABB.Width + ShuttleSpawnBuffer;
       component.EmergencyShuttle = shuttle;
       EnsureComp<ProtectedGridComponent>(shuttle.Value);
   }

   private void CleanupEmergencyShuttle()
   {
       // If we get cleaned up mid roundend just end it.
       if (_launchedShuttles)
       {
           _roundEnd.EndRound();
       }

       _shuttleIndex = 0f;

       if (CentComMap == null || !_mapManager.MapExists(CentComMap.Value))
       {
           CentComMap = null;
           return;
       }

       _mapManager.DeleteMap(CentComMap.Value);
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
