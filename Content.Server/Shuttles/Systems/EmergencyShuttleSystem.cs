using System.Numerics;
using System.Threading;
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
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Events;
using Content.Shared.Tag;
using Content.Shared.Tiles;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

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

    private const float ShuttleSpawnBuffer = 1f;

    private bool _emergencyShuttleEnabled;

    [ValidatePrototypeId<TagPrototype>]
    private const string DockTag = "DockEmergency";

    public override void Initialize()
    {
        _sawmill = Logger.GetSawmill("shuttle.emergency");
        _emergencyShuttleEnabled = _configManager.GetCVar(CCVars.EmergencyShuttleEnabled);
        // Don't immediately invoke as roundstart will just handle it.
        _configManager.OnValueChanged(CCVars.EmergencyShuttleEnabled, SetEmergencyShuttleEnabled);
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
        SubscribeLocalEvent<StationEmergencyShuttleComponent, ComponentStartup>(OnStationStartup);
        SubscribeLocalEvent<StationCentcommComponent, ComponentShutdown>(OnCentcommShutdown);
        SubscribeLocalEvent<StationCentcommComponent, ComponentInit>(OnCentcommInit);
        SubscribeNetworkEvent<EmergencyShuttleRequestPositionMessage>(OnShuttleRequestPosition);
        InitializeEmergencyConsole();
    }

    private void OnRoundStart(RoundStartingEvent ev)
    {
        CleanupEmergencyConsole();
        _roundEndCancelToken?.Cancel();
        _roundEndCancelToken = new CancellationTokenSource();
    }

    private void OnCentcommShutdown(EntityUid uid, StationCentcommComponent component, ComponentShutdown args)
    {
        QueueDel(component.Entity);
        component.Entity = EntityUid.Invalid;

        if (_mapManager.MapExists(component.MapId))
            _mapManager.DeleteMap(component.MapId);

        component.MapId = MapId.Nullspace;
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

    private void CleanupEmergencyShuttle()
    {
        var query = AllEntityQuery<StationCentcommComponent>();

        while (query.MoveNext(out var uid, out _))
        {
            RemCompDeferred<StationCentcommComponent>(uid);
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
            StationUid = GetNetEntity(targetGrid),
            Position = config.Area,
        });
    }

    /// <summary>
    /// Calls the emergency shuttle for the station.
    /// </summary>
    public void CallEmergencyShuttle(EntityUid stationUid, StationEmergencyShuttleComponent? stationShuttle = null)
    {
        if (!Resolve(stationUid, ref stationShuttle) ||
            !TryComp<TransformComponent>(stationShuttle.EmergencyShuttle, out var xform) ||
            !TryComp<ShuttleComponent>(stationShuttle.EmergencyShuttle, out var shuttle))
        {
            return;
        }

        var targetGrid = _station.GetLargestGrid(Comp<StationDataComponent>(stationUid));

        // UHH GOOD LUCK
        if (targetGrid == null)
        {
            _logger.Add(LogType.EmergencyShuttle, LogImpact.High, $"Emergency shuttle {ToPrettyString(stationUid)} unable to dock with station {ToPrettyString(stationUid)}");
            _chatSystem.DispatchStationAnnouncement(stationUid, Loc.GetString("emergency-shuttle-good-luck"), playDefaultSound: false);
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
                _chatSystem.DispatchStationAnnouncement(stationUid, Loc.GetString("emergency-shuttle-docked", ("time", $"{_consoleAccumulator:0}"), ("direction", angle.GetDir())), playDefaultSound: false);
            }

            _logger.Add(LogType.EmergencyShuttle, LogImpact.High, $"Emergency shuttle {ToPrettyString(stationUid)} docked with stations");
            // TODO: Need filter extensions or something don't blame me.
            _audio.PlayGlobal("/Audio/Announcements/shuttle_dock.ogg", Filter.Broadcast(), true);
        }
        else
        {
            if (TryComp<TransformComponent>(targetGrid.Value, out var targetXform))
            {
                var angle = _dock.GetAngle(stationShuttle.EmergencyShuttle.Value, xform, targetGrid.Value, targetXform, xformQuery);
                _chatSystem.DispatchStationAnnouncement(stationUid, Loc.GetString("emergency-shuttle-nearby", ("direction", angle.GetDir())), playDefaultSound: false);
            }

            _logger.Add(LogType.EmergencyShuttle, LogImpact.High, $"Emergency shuttle {ToPrettyString(stationUid)} unable to find a valid docking port for {ToPrettyString(stationUid)}");
            // TODO: Need filter extensions or something don't blame me.
            _audio.PlayGlobal("/Audio/Misc/notice1.ogg", Filter.Broadcast(), true);
        }
    }

    private void OnCentcommInit(EntityUid uid, StationCentcommComponent component, ComponentInit args)
    {
        if (!_emergencyShuttleEnabled)
            return;

        // Post mapinit? fancy
        if (TryComp<TransformComponent>(component.Entity, out var xform))
        {
            component.MapId = xform.MapID;
            return;
        }

        AddCentcomm(component);
    }

    private void OnStationStartup(EntityUid uid, StationEmergencyShuttleComponent component, ComponentStartup args)
    {
        AddEmergencyShuttle(uid, component);
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

        var query = AllEntityQuery<StationEmergencyShuttleComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            CallEmergencyShuttle(uid, comp);
        }

        _commsConsole.UpdateCommsConsoleInterface();
    }

    private void SetupEmergencyShuttle()
    {
        if (!_emergencyShuttleEnabled)
            return;

        var centcommQuery = AllEntityQuery<StationCentcommComponent>();

        while (centcommQuery.MoveNext(out var centcomm))
        {
            AddCentcomm(centcomm);
        }

        var query = AllEntityQuery<StationEmergencyShuttleComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            AddEmergencyShuttle(uid, comp);
        }
    }

    private void AddCentcomm(StationCentcommComponent component)
    {
        // Check for existing centcomms and just point to that
        var query = AllEntityQuery<StationCentcommComponent>();

        while (query.MoveNext(out var otherComp))
        {
            if (otherComp == component)
                continue;

            component.MapId = otherComp.MapId;
            component.ShuttleIndex = otherComp.ShuttleIndex;
            return;
        }

        var mapId = _mapManager.CreateMap();
        component.MapId = mapId;

        if (!string.IsNullOrEmpty(component.Map.ToString()))
        {
            var ent = _map.LoadGrid(mapId, component.Map.ToString());

            if (ent != null)
            {
                component.Entity = ent.Value;
                _shuttle.AddFTLDestination(ent.Value, false);
            }
        }
        else
        {
            _sawmill.Warning("No CentComm map found, skipping setup.");
        }
    }

    public HashSet<MapId> GetCentcommMaps()
    {
        var query = AllEntityQuery<StationCentcommComponent>();
        var maps = new HashSet<MapId>(Count<StationCentcommComponent>());

        while (query.MoveNext(out var comp))
        {
            maps.Add(comp.MapId);
        }

        return maps;
    }

    private void AddEmergencyShuttle(EntityUid uid, StationEmergencyShuttleComponent component)
    {
        if (!_emergencyShuttleEnabled
            || component.EmergencyShuttle != null ||
            !TryComp<StationCentcommComponent>(uid, out var centcomm))
        {
            return;
        }

        // Load escape shuttle
        var shuttlePath = component.EmergencyShuttlePath;
        var shuttle = _map.LoadGrid(centcomm.MapId, shuttlePath.ToString(), new MapLoadOptions()
        {
            // Should be far enough... right? I'm too lazy to bounds check CentCom rn.
            Offset = new Vector2(500f + centcomm.ShuttleIndex, 0f)
        });

        if (shuttle == null)
        {
            _sawmill.Error($"Unable to spawn emergency shuttle {shuttlePath} for {ToPrettyString(uid)}");
            return;
        }

        centcomm.ShuttleIndex += _mapManager.GetGrid(shuttle.Value).LocalAABB.Width + ShuttleSpawnBuffer;

        // Update indices for all centcomm comps pointing to same map
        var query = AllEntityQuery<StationCentcommComponent>();

        while (query.MoveNext(out var comp))
        {
            if (comp == centcomm || comp.MapId != centcomm.MapId)
                continue;

            comp.ShuttleIndex = centcomm.ShuttleIndex;
        }

        component.EmergencyShuttle = shuttle;
        EnsureComp<ProtectedGridComponent>(shuttle.Value);
        EnsureComp<PreventPilotComponent>(shuttle.Value);
    }

    private void OnEscapeUnpaused(EntityUid uid, EscapePodComponent component, ref EntityUnpausedEvent args)
    {
        if (component.LaunchTime == null)
            return;

        component.LaunchTime = component.LaunchTime.Value + args.PausedTime;
    }

    /// <summary>
    /// Returns whether a target is escaping on the emergency shuttle, but only if evac has arrived.
    /// </summary>
    public bool IsTargetEscaping(EntityUid target)
    {
        // if evac isn't here then sitting in a pod doesn't return true
        if (!EmergencyShuttleArrived)
            return false;

        // check each emergency shuttle
        var xform = Transform(target);
        foreach (var stationData in EntityQuery<StationEmergencyShuttleComponent>())
        {
            if (stationData.EmergencyShuttle == null)
                continue;

            if (IsOnGrid(xform, stationData.EmergencyShuttle.Value))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsOnGrid(TransformComponent xform, EntityUid shuttle, MapGridComponent? grid = null, TransformComponent? shuttleXform = null)
    {
        if (!Resolve(shuttle, ref grid, ref shuttleXform))
            return false;

        return shuttleXform.WorldMatrix.TransformBox(grid.LocalAABB).Contains(xform.WorldPosition);
    }
}
