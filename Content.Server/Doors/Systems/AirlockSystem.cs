using Content.Server.AlertLevel;
using Content.Server.Chat.Systems;
using Content.Server.DeviceLinking.Events;
using Content.Server.Power.Components;
using Content.Server.Station.Systems;
using Content.Server.Wires;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Interaction;
using Content.Shared.Wires;
using Robust.Shared.Player;

namespace Content.Server.Doors.Systems;

public sealed class AirlockSystem : SharedAirlockSystem
{
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly WiresSystem _wiresSystem = default!;
    [Dependency] private readonly StationSystem _station = default!;

    private static bool _hasNotifiedEAChanges = false;
    private static bool _previousDeltaEA = false;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AirlockComponent, ComponentInit>(OnAirlockInit);
        SubscribeLocalEvent<AirlockComponent, SignalReceivedEvent>(OnSignalReceived);

        SubscribeLocalEvent<AirlockComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<AirlockComponent, ActivateInWorldEvent>(OnActivate, before: new[] { typeof(DoorSystem) });
        SubscribeLocalEvent<AlertLevelChangedEvent>(OnAlertLevelChanged);
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AirlockComponent>();
        while (query.MoveNext(out var uid, out var airlock))
        {

            if (airlock.DeltaAlertOngoing && !_hasNotifiedEAChanges && airlock.DeltaAlertRemainingEmergencyAccessTimer <= 0)
            {
                _hasNotifiedEAChanges = true;
                _previousDeltaEA = true;
                // Play a station-wide announcement indicating that all doors have had their locks disabled and urge the players to 'evacuate'.
                var airlockXform = Transform(uid);
                var stationUid = _station.GetStationInMap(airlockXform.MapID);
                var announcement = Loc.GetString("alert-level-delta-emergencyaccess");
                _chatSystem.DispatchStationAnnouncement(stationUid ?? uid, announcement, null, false, null, Color.Yellow);
            }
        }
    }

    private void OnAlertLevelChanged(AlertLevelChangedEvent ev)
    {
        Log.Info("AlertLevelReceived");
        if (ev.AlertLevel == "delta")
        {
            var query = EntityQueryEnumerator<AirlockComponent>();
            while (query.MoveNext(out var uid, out var airlock))
            {

                var airlockXform = Transform(uid);
                var stationUid = _station.GetStationInMap(airlockXform.MapID);
                if (stationUid != ev.Station) continue;
                airlock.DeltaAlertOngoing = true;
                // Immediately EA doors if we have had the emergency access flag trigger prior; this
                // helps in cases such as a nuke being defused, disarmed, and later armed again.
                if (_previousDeltaEA)
                    airlock.DeltaAlertRemainingEmergencyAccessTimer = 0;
                else
                    airlock.DeltaAlertRemainingEmergencyAccessTimer = airlock.DeltaAlertEmergencyAccessDelayTime;
                airlock.DeltaAlertRecentlyEnded = false;
            }
        }
        else
        {
            _hasNotifiedEAChanges = false;
            var query = EntityQueryEnumerator<AirlockComponent>();
            while (query.MoveNext(out var uid, out var airlock))
            {

                var airlockXform = Transform(uid);
                var stationUid = _station.GetStationInMap(airlockXform.MapID);
                if (stationUid != ev.Station) continue;

                if (airlock.DeltaAlertOngoing)
                {
                    airlock.DeltaAlertRecentlyEnded = true;
                    airlock.PostDeltaAlertRemainingEmergencyAccessTimer = airlock.PostDeltaAlertEmergencyAccessTimer;
                }
                airlock.DeltaAlertOngoing = false;
            }
        }
    }

    private void OnAirlockInit(EntityUid uid, AirlockComponent component, ComponentInit args)
    {
        if (TryComp<ApcPowerReceiverComponent>(uid, out var receiverComponent))
        {
            Appearance.SetData(uid, DoorVisuals.Powered, receiverComponent.Powered);
        }
    }

    private void OnSignalReceived(EntityUid uid, AirlockComponent component, ref SignalReceivedEvent args)
    {
        if (args.Port == component.AutoClosePort)
        {
            component.AutoClose = false;
        }
    }

    private void OnPowerChanged(EntityUid uid, AirlockComponent component, ref PowerChangedEvent args)
    {
        component.Powered = args.Powered;
        Dirty(uid, component);

        if (TryComp<AppearanceComponent>(uid, out var appearanceComponent))
        {
            Appearance.SetData(uid, DoorVisuals.Powered, args.Powered, appearanceComponent);
        }

        if (!TryComp(uid, out DoorComponent? door))
            return;

        if (!args.Powered)
        {
            // stop any scheduled auto-closing
            if (door.State == DoorState.Open)
                DoorSystem.SetNextStateChange(uid, null);
        }
        else
        {
            UpdateAutoClose(uid, door: door);
        }
    }

    private void OnActivate(EntityUid uid, AirlockComponent component, ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        if (TryComp<WiresPanelComponent>(uid, out var panel) &&
            panel.Open &&
            TryComp<ActorComponent>(args.User, out var actor))
        {
            if (TryComp<WiresPanelSecurityComponent>(uid, out var wiresPanelSecurity) &&
                !wiresPanelSecurity.WiresAccessible)
                return;

            _wiresSystem.OpenUserInterface(uid, actor.PlayerSession);
            args.Handled = true;
            return;
        }

        if (component.KeepOpenIfClicked)
        {
            // Disable auto close
            component.AutoClose = false;
        }
    }
}
