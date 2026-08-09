using System.Linq;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Power.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared.Database;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Medical.CrewMonitoring;
using Content.Shared.Medical.SuitSensor;
using Content.Shared.Pinpointer;
using Content.Shared.Popups;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Medical.CrewMonitoring;

public sealed class CrewMonitoringConsoleSystem : EntitySystem
{
    // DS14-start
    private static readonly CrewMonitoringConsolePingMode[] SelectablePingModes =
    [
        CrewMonitoringConsolePingMode.Severe,
        CrewMonitoringConsolePingMode.Critical,
        CrewMonitoringConsolePingMode.Dead,
        CrewMonitoringConsolePingMode.Disabled,
    ];

    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly PowerReceiverSystem _power = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    // DS14-end
    [Dependency] private readonly PowerCellSystem _cell = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    // DS14-start
    [Dependency] private readonly SharedStationAiSystem _stationAi = default!;
    [Dependency] private readonly StationSystem _station = default!;
    // DS14-end

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, BoundUIOpenedEvent>(OnUIOpened);
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, GetVerbsEvent<Verb>>(OnGetVerb); // DS14
    }

    private void OnRemove(EntityUid uid, CrewMonitoringConsoleComponent component, ComponentRemove args)
    {
        component.ConnectedSensors.Clear();
    }

    // DS14-start
    private void TryPlayPing(Entity<CrewMonitoringConsoleComponent> ent, CrewMonitoringConsolePingMode pingMode)
    {
        if (HasComp<ActorComponent>(ent.Owner) ||
            ent.Comp.CurrentPingMode == CrewMonitoringConsolePingMode.Disabled ||
            ent.Comp.CurrentPingMode > pingMode)
        {
            return;
        }

        var curTime = _timing.CurTime;
        if (ent.Comp.NextSound > curTime)
            return;

        if (HasComp<PowerCellDrawComponent>(ent.Owner))
        {
            //6.6f это примерно 2% у маленькой батареи. При обычном (20f) 6% маленькой батареи
            if (!_cell.TryUseCharge(ent.Owner, 6.6f))
                return;
        }
        else if (!_power.IsPowered(ent.Owner))
        {
            return;
        }

        ent.Comp.NextSound = curTime + ent.Comp.SoundInterval;

        var popup = Loc.GetString("crew-monitoring-console-ping",
            ("monitor", MetaData(ent.Owner).EntityName));
        _popup.PopupEntity(popup, ent.Owner, PopupType.Medium);
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/beep1.ogg"), ent.Owner);
    }
    // DS14-end

    private void OnPacketReceived(EntityUid uid, CrewMonitoringConsoleComponent component, DeviceNetworkPacketEvent args)
    {
        var payload = args.Data;

        // DS14-start
        if (payload.TryGetValue("PingMode", out CrewMonitoringConsolePingMode pingMode))
            TryPlayPing((uid, component), pingMode);
        // DS14-end

        // Check command
        if (!payload.TryGetValue(DeviceNetworkConstants.Command, out string? command))
            return;

        if (command != DeviceNetworkConstants.CmdUpdatedState)
            return;

        if (!payload.TryGetValue(SuitSensorConstants.NET_STATUS_COLLECTION, out Dictionary<string, SuitSensorStatus>? sensorStatus))
            return;

        component.ConnectedSensors = sensorStatus;
        UpdateUserInterface(uid, component);
    }

    private void OnUIOpened(EntityUid uid, CrewMonitoringConsoleComponent component, BoundUIOpenedEvent args)
    {
        if (!_cell.TryUseActivatableCharge(uid))
            return;

        TryUpdateStationAiFallback(uid, component); // DS14
        UpdateUserInterface(uid, component);
    }

    // DS14-start
    private void TryUpdateStationAiFallback(EntityUid uid, CrewMonitoringConsoleComponent component)
    {
        if (component.ConnectedSensors.Count != 0 ||
            !HasComp<StationAiHeldComponent>(uid) ||
            !_stationAi.TryGetCore(uid, out var core) ||
            core.Comp == null ||
            _station.GetOwningStation(core.Owner) is not { } station)
        {
            return;
        }

        var query = EntityQueryEnumerator<CrewMonitoringServerComponent, SingletonDeviceNetServerComponent>();
        while (query.MoveNext(out var serverUid, out var server, out var singleton))
        {
            if (!singleton.Active ||
                _station.GetOwningStation(serverUid) != station)
            {
                continue;
            }

            component.ConnectedSensors = new Dictionary<string, SuitSensorStatus>(server.SensorStatus);
            return;
        }
    }
    // DS14-end

    private void UpdateUserInterface(EntityUid uid, CrewMonitoringConsoleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!_uiSystem.IsUiOpen(uid, CrewMonitoringUIKey.Key))
            return;

        // The grid must have a NavMapComponent to visualize the map in the UI
        var xform = Transform(uid);

        if (xform.GridUid != null)
            EnsureComp<NavMapComponent>(xform.GridUid.Value);

        // Update all sensors info
        var allSensors = component.ConnectedSensors.Values.ToList();
        _uiSystem.SetUiState(uid, CrewMonitoringUIKey.Key, new CrewMonitoringState(allSensors));
    }

    // DS14-start
    private void OnGetVerb(EntityUid uid, CrewMonitoringConsoleComponent component, GetVerbsEvent<Verb> args)
    {
        if (HasComp<ActorComponent>(uid) ||
            !args.CanAccess ||
            !args.CanInteract ||
            !args.CanComplexInteract)
        {
            return;
        }

        for (var i = 0; i < SelectablePingModes.Length; i++)
        {
            var pingMode = SelectablePingModes[i];
            var text = GetTextByMode(pingMode);

            args.Verbs.Add(new Verb
            {
                Priority = SelectablePingModes.Length - i,
                Icon = GetSpriteByMode(pingMode),
                Disabled = pingMode == component.CurrentPingMode,
                Category = VerbCategory.PingSelect,
                Text = text,
                Impact = LogImpact.Low,
                DoContactInteraction = false,
                CloseMenu = true,
                Act = () => SetPingMode((uid, component), pingMode, text, args.User),
            });
        }
    }

    private void SetPingMode(
        Entity<CrewMonitoringConsoleComponent> ent,
        CrewMonitoringConsolePingMode pingMode,
        string text,
        EntityUid user)
    {
        ent.Comp.CurrentPingMode = pingMode;
        _popup.PopupEntity(
            Loc.GetString("crew-monitoring-console-ping-mode-set", ("mode", text)),
            ent.Owner,
            user);
    }

    private static SpriteSpecifier? GetSpriteByMode(CrewMonitoringConsolePingMode mode)
    {
        return mode switch
        {
            CrewMonitoringConsolePingMode.Severe =>
                new SpriteSpecifier.Rsi(new ResPath("Interface/Alerts/human_crew_monitoring.rsi"), "health4"),
            CrewMonitoringConsolePingMode.Critical =>
                new SpriteSpecifier.Rsi(new ResPath("Interface/Alerts/human_crew_monitoring.rsi"), "critical"),
            CrewMonitoringConsolePingMode.Dead =>
                new SpriteSpecifier.Rsi(new ResPath("Interface/Alerts/human_crew_monitoring.rsi"), "dead"),
            _ => null,
        };
    }

    private string GetTextByMode(CrewMonitoringConsolePingMode mode)
    {
        var key = mode switch
        {
            CrewMonitoringConsolePingMode.Severe => "crew-monitoring-console-ping-mode-severe",
            CrewMonitoringConsolePingMode.Critical => "crew-monitoring-console-ping-mode-critical",
            CrewMonitoringConsolePingMode.Dead => "crew-monitoring-console-ping-mode-dead",
            CrewMonitoringConsolePingMode.Disabled => "crew-monitoring-console-ping-mode-disabled",
            _ => "crew-monitoring-console-ping-mode-unknown",
        };

        return Loc.GetString(key);
    }
    // DS14-end
}
