using System.Diagnostics;
using Content.Server.Access.Systems;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Communications;
using Content.Server.GameTicking.Events;
using Content.Server.Popups;
using Content.Server.RoundEnd;
using Content.Server.Shuttles.Components;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Access.Systems;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Shuttles.Events;
using Content.Shared.Tiles;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Shuttles.Systems;

public sealed partial class EmergencyShuttleSystem : EntitySystem
{
   /*
    * Handles the escape shuttle + CentCom.
    */

   [Dependency] private readonly IAdminLogManager _logger = default!;
   [Dependency] private readonly IAdminManager _admin = default!;
   [Dependency] private readonly IConfigurationManager _configManager = default!;
   [Dependency] private readonly IGameTiming _timing = default!;
   [Dependency] private readonly IMapManager _mapManager = default!;
   [Dependency] private readonly IRobustRandom _random = default!;
   [Dependency] private readonly AccessReaderSystem _reader = default!;
   [Dependency] private readonly ChatSystem _chatSystem = default!;
   [Dependency] private readonly CommunicationsConsoleSystem _commsConsole = default!;
   [Dependency] private readonly DockingSystem _dock = default!;
   [Dependency] private readonly IdCardSystem _idSystem = default!;
   [Dependency] private readonly MapLoaderSystem _map = default!;
   [Dependency] private readonly PopupSystem _popup = default!;
   [Dependency] private readonly RoundEndSystem _roundEnd = default!;
   [Dependency] private readonly SharedAudioSystem _audio = default!;
   [Dependency] private readonly ShuttleSystem _shuttle = default!;
   [Dependency] private readonly StationSystem _station = default!;
   [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

   private ISawmill _sawmill = default!;

   public MapId? CentComMap { get; private set; }
   public EntityUid? CentComGrid { get; private set; }

   /// <summary>
   /// Used for multiple shuttle spawn offsets.
   /// </summary>
   private float _shuttleIndex;

   private const float ShuttleSpawnBuffer = 1f;

   private bool _emergencyShuttleEnabled;

   private const string DockTag = "DockEmergency";

   public override void Initialize()
   {
       _sawmill = Logger.GetSawmill("shuttle.emergency");
       _emergencyShuttleEnabled = _configManager.GetCVar(CCVars.EmergencyShuttleEnabled);
       // Don't immediately invoke as roundstart will just handle it.
       _configManager.OnValueChanged(CCVars.EmergencyShuttleEnabled, SetEmergencyShuttleEnabled);
       SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
       SubscribeLocalEvent<StationEmergencyShuttleComponent, ComponentStartup>(OnStationStartup);
       SubscribeNetworkEvent<EmergencyShuttleRequestPositionMessage>(OnShuttleRequestPosition);
       InitializeEmergencyConsole();
   }

   private void SetEmergencyShuttleEnabled(bool value)
   {
       if (_emergencyShuttleEnabled == value)
           return;
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

   public override void Update(float frameTime)
   {
       base.Update(frameTime);
       UpdateEmergencyConsole(frameTime);
   }

   public override void Shutdown()
   {
       _configManager.UnsubValueChanged(CCVars.EmergencyShuttleEnabled, SetEmergencyShuttleEnabled);
       ShutdownEmergencyConsole();
   }

   /// <summary>
   /// If the client is requesting debug info on where an emergency shuttle would dock.
   /// </summary>
   private void OnShuttleRequestPosition(EmergencyShuttleRequestPositionMessage msg, EntitySessionEventArgs args)
   {
       if (!_admin.IsAdmin((IPlayerSession) args.SenderSession))
           return;

       var player = args.SenderSession.AttachedEntity;
       if (player is null)
           return;

       var station = _station.GetOwningStation(player.Value);

       if (!TryComp<StationEmergencyShuttleComponent>(station, out var stationShuttle) ||
           !HasComp<ShuttleComponent>(stationShuttle.EmergencyShuttle))
       {
           return;
       }

       var targetGrid = _station.GetLargestGrid(Comp<StationDataComponent>(station.Value));
       if (targetGrid == null)
           return;

       var config = _dock.GetDockingConfig(stationShuttle.EmergencyShuttle.Value, targetGrid.Value, DockTag);
       if (config == null)
           return;

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
       if (!TryComp<StationEmergencyShuttleComponent>(stationUid, out var stationShuttle) ||
           !TryComp<TransformComponent>(stationShuttle.EmergencyShuttle, out var xform) ||
           !TryComp<ShuttleComponent>(stationShuttle.EmergencyShuttle, out var shuttle))
       {
           return;
       }

       var targetGrid = _station.GetLargestGrid(Comp<StationDataComponent>(stationUid.Value));

       // UHH GOOD LUCK
       if (targetGrid == null)
       {
           _logger.Add(LogType.EmergencyShuttle, LogImpact.High, $"Emergency shuttle {ToPrettyString(stationUid.Value)} unable to dock with station {ToPrettyString(stationUid.Value)}");
           _chatSystem.DispatchStationAnnouncement(stationUid.Value, Loc.GetString("emergency-shuttle-good-luck"), playDefaultSound: false);
           // TODO: Need filter extensions or something don't blame me.
           _audio.PlayGlobal("/Audio/Misc/notice1.ogg", Filter.Broadcast(), true);
           return;
       }

       var xformQuery = GetEntityQuery<TransformComponent>();

       if (_shuttle.TryFTLDock(stationShuttle.EmergencyShuttle.Value, shuttle, targetGrid.Value, DockTag))
       {
           if (TryComp<TransformComponent>(targetGrid.Value, out var targetXform))
           {
               var angle = _dock.GetAngle(stationShuttle.EmergencyShuttle.Value, xform, targetGrid.Value, targetXform, xformQuery);
               _chatSystem.DispatchStationAnnouncement(stationUid.Value, Loc.GetString("emergency-shuttle-docked", ("time", $"{_consoleAccumulator:0}"), ("direction", angle.GetDir())), playDefaultSound: false);
           }

           _logger.Add(LogType.EmergencyShuttle, LogImpact.High, $"Emergency shuttle {ToPrettyString(stationUid.Value)} docked with stations");
           // TODO: Need filter extensions or something don't blame me.
           _audio.PlayGlobal("/Audio/Announcements/shuttle_dock.ogg", Filter.Broadcast(), true);
       }
       else
       {
           if (TryComp<TransformComponent>(targetGrid.Value, out var targetXform))
           {
               var angle = _dock.GetAngle(stationShuttle.EmergencyShuttle.Value, xform, targetGrid.Value, targetXform, xformQuery);
               _chatSystem.DispatchStationAnnouncement(stationUid.Value, Loc.GetString("emergency-shuttle-nearby", ("direction", angle.GetDir())), playDefaultSound: false);
           }

           _logger.Add(LogType.EmergencyShuttle, LogImpact.High, $"Emergency shuttle {ToPrettyString(stationUid.Value)} unable to find a valid docking port for {ToPrettyString(stationUid.Value)}");
           // TODO: Need filter extensions or something don't blame me.
           _audio.PlayGlobal("/Audio/Misc/notice1.ogg", Filter.Broadcast(), true);
       }
   }

   private void OnStationStartup(EntityUid uid, StationEmergencyShuttleComponent component, ComponentStartup args)
   {
       AddEmergencyShuttle(component);
   }

   private void OnRoundStart(RoundStartingEvent ev)
   {
       if (CentComMap != null)
           _mapManager.DeleteMap(CentComMap.Value);

       // Just in case the grid isn't on the map.
       DebugTools.Assert(Deleted(CentComGrid));
       if (CentComGrid != null)
           Del(CentComGrid.Value);

       CentComGrid = null;
       CentComMap = null;

       CleanupEmergencyConsole();
       SetupEmergencyShuttle();
   }

   /// <summary>
   /// Spawns the emergency shuttle for each station and starts the countdown until controls unlock.
   /// </summary>
   public void CallEmergencyShuttle()
   {
       if (EmergencyShuttleArrived)
           return;

       if (!_emergencyShuttleEnabled)
       {
           _roundEnd.EndRound();
           return;
       }

       _consoleAccumulator = _configManager.GetCVar(CCVars.EmergencyShuttleDockTime);
       EmergencyShuttleArrived = true;

       if (CentComMap != null)
         _mapManager.SetMapPaused(CentComMap.Value, false);

       var query = AllEntityQuery<StationDataComponent>();

       while (query.MoveNext(out var uid, out var comp))
       {
           CallEmergencyShuttle(uid);
       }

       _commsConsole.UpdateCommsConsoleInterface();
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
           CentComGrid = centcomm;

           if (CentComGrid != null)
               _shuttle.AddFTLDestination(CentComGrid.Value, false);
       }
       else
       {
           _sawmill.Info("No CentCom map found, skipping setup.");
       }

       foreach (var comp in EntityQuery<StationEmergencyShuttleComponent>(true))
       {
           AddEmergencyShuttle(comp);
       }
   }

   private void AddEmergencyShuttle(StationEmergencyShuttleComponent component)
   {
       if (!_emergencyShuttleEnabled
           || CentComMap == null
           || component.EmergencyShuttle != null)
       {
           return;
       }

       // Load escape shuttle
       var shuttlePath = component.EmergencyShuttlePath;
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

   private void OnEscapeUnpaused(EntityUid uid, EscapePodComponent component, ref EntityUnpausedEvent args)
   {
       if (component.LaunchTime == null)
           return;

       component.LaunchTime = component.LaunchTime.Value + args.PausedTime;
   }
}
