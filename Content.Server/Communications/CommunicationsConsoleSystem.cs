using Content.Server.Administration.Logs;
using Content.Server.AlertLevel;
using Content.Server.Chat.Systems;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Interaction;
using Content.Server.Popups;
using Content.Server.RoundEnd;
using Content.Server.Screens.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Communications;
using Content.Shared.Database;
using Content.Shared.DeviceNetwork;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Content.Server.DeadSpace.ERTCall;
using Content.Shared.Containers.ItemSlots;
using System.Linq;
using Robust.Shared.Containers;
using Content.Server.Chat.Managers;

namespace Content.Server.Communications
{
    public sealed class CommunicationsConsoleSystem : EntitySystem
    {
        [Dependency] private readonly AccessReaderSystem _accessReaderSystem = default!;
        [Dependency] private readonly AlertLevelSystem _alertLevelSystem = default!;
        [Dependency] private readonly ChatSystem _chatSystem = default!;
        [Dependency] private readonly DeviceNetworkSystem _deviceNetworkSystem = default!;
        [Dependency] private readonly EmergencyShuttleSystem _emergency = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
        [Dependency] private readonly StationSystem _stationSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly ERTCallSystem _ertCallSystem = default!;
        [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;

        private const float UIUpdateInterval = 5.0f;

        public override void Initialize()
        {
            SubscribeLocalEvent<CommunicationsConsoleComponent, MapInitEvent>(OnMapInit);

            // All events that refresh the BUI
            SubscribeLocalEvent<AlertLevelChangedEvent>(OnAlertLevelChanged);
            SubscribeLocalEvent<CommunicationsConsoleComponent, ComponentInit>((uid, comp, _) => UpdateCommsConsoleInterface(uid, comp));
            SubscribeLocalEvent<RoundEndSystemChangedEvent>(_ => OnGenericBroadcastEvent());
            SubscribeLocalEvent<AlertLevelDelayFinishedEvent>(_ => OnGenericBroadcastEvent());
            SubscribeLocalEvent<ERTCallEvent>(OnERTCall);
            SubscribeLocalEvent<ERTRecallEvent>(OnERTRecall);

            // Messages from the BUI
            SubscribeLocalEvent<CommunicationsConsoleComponent, CommunicationsConsoleSetAlertLevelMessage>(OnSetAlertLevelMessage);
            SubscribeLocalEvent<CommunicationsConsoleComponent, CommunicationsConsoleSelectAlertLevelMessage>(OnSelectAlertLevelMessage);
            SubscribeLocalEvent<CommunicationsConsoleComponent, CommunicationsConsoleAnnounceMessage>(OnAnnounceMessage);
            SubscribeLocalEvent<CommunicationsConsoleComponent, CommunicationsConsoleBroadcastMessage>(OnBroadcastMessage);
            SubscribeLocalEvent<CommunicationsConsoleComponent, CommunicationsConsoleCallEmergencyShuttleMessage>(OnCallShuttleMessage);
            SubscribeLocalEvent<CommunicationsConsoleComponent, CommunicationsConsoleRecallEmergencyShuttleMessage>(OnRecallShuttleMessage);

            SubscribeLocalEvent<CommunicationsConsoleComponent, CommunicationsConsoleCallERTMessage>(OnERTCallMessage);
            SubscribeLocalEvent<CommunicationsConsoleComponent, CommunicationsConsoleRecallERTMessage>(OnERTRecallMessage);
            SubscribeLocalEvent<CommunicationsConsoleComponent, CommunicationsConsoleSelectERTMessage>(OnERTSelectMessage);

            SubscribeLocalEvent<CommunicationsConsoleComponent, EntInsertedIntoContainerMessage>((uid, comp, _) => UpdateCommsConsoleInterface(uid, comp));
            SubscribeLocalEvent<CommunicationsConsoleComponent, EntRemovedFromContainerMessage>((uid, comp, _) => UpdateCommsConsoleInterface(uid, comp));

        }

        public void OnMapInit(EntityUid uid, CommunicationsConsoleComponent comp, MapInitEvent args)
        {
            comp.AnnouncementCooldownRemaining = comp.InitialDelay;

            _itemSlotsSystem.AddItemSlot(uid, CommunicationsConsoleComponent.FirstPrivilegedSlotId, comp.FirstPrivilegedIdSlot);
            _itemSlotsSystem.AddItemSlot(uid, CommunicationsConsoleComponent.SecondPrivilegedSlotId, comp.SecondPrivilegedIdSlot);
        }

        public override void Update(float frameTime)
        {
            var query = EntityQueryEnumerator<CommunicationsConsoleComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                // TODO refresh the UI in a less horrible way
                if (comp.AnnouncementCooldownRemaining >= 0f)
                {
                    comp.AnnouncementCooldownRemaining -= frameTime;
                }

                // TODO also refresh the UI in a less horrible way
                if (comp.CallERTCooldownRemaining >= 0f)
                {
                    comp.CallERTCooldownRemaining -= frameTime;
                }

                comp.UIUpdateAccumulator += frameTime;

                if (comp.UIUpdateAccumulator < UIUpdateInterval)
                    continue;

                comp.UIUpdateAccumulator -= UIUpdateInterval;

                if (_uiSystem.IsUiOpen(uid, CommunicationsConsoleUiKey.Key))
                    UpdateCommsConsoleInterface(uid, comp);
            }

            base.Update(frameTime);
        }

        /// <summary>
        /// Update the UI of every comms console.
        /// </summary>
        private void OnGenericBroadcastEvent()
        {
            var query = EntityQueryEnumerator<CommunicationsConsoleComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                UpdateCommsConsoleInterface(uid, comp);
            }
        }

        /// <summary>
        /// Updates all comms consoles belonging to the station that the alert level was set on
        /// </summary>
        /// <param name="args">Alert level changed event arguments</param>
        private void OnAlertLevelChanged(AlertLevelChangedEvent args)
        {
            var query = EntityQueryEnumerator<CommunicationsConsoleComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                var entStation = _stationSystem.GetOwningStation(uid);
                if (args.Station == entStation)
                    UpdateCommsConsoleInterface(uid, comp);
            }
        }

        private void OnERTCall(ERTCallEvent args)
        {
            var query = EntityQueryEnumerator<CommunicationsConsoleComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                var entStation = _stationSystem.GetOwningStation(uid);
                if (args.Station == entStation)
                    UpdateCommsConsoleInterface(uid, comp);
            }
        }

        private void OnERTRecall(ERTRecallEvent args)
        {
            var query = EntityQueryEnumerator<CommunicationsConsoleComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                var entStation = _stationSystem.GetOwningStation(uid);
                if (args.Station == entStation)
                    UpdateCommsConsoleInterface(uid, comp);
            }
        }

        /// <summary>
        /// Updates the UI for all comms consoles.
        /// </summary>
        public void UpdateCommsConsoleInterface()
        {
            var query = EntityQueryEnumerator<CommunicationsConsoleComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                UpdateCommsConsoleInterface(uid, comp);
            }
        }

        /// <summary>
        /// Updates the UI for a particular comms console.
        /// </summary>
        public void UpdateCommsConsoleInterface(EntityUid uid, CommunicationsConsoleComponent comp)
        {
            var stationUid = _stationSystem.GetOwningStation(uid);
            List<string>? levels = null;
            string currentLevel = default!;
            float currentDelay = 0;

            List<string>? ertList = null;

            if (stationUid != null)
            {
                if (TryComp(stationUid.Value, out AlertLevelComponent? alertComp) &&
                    alertComp.AlertLevels != null)
                {
                    if (alertComp.IsSelectable)
                    {
                        levels = new();
                        foreach (var (id, detail) in alertComp.AlertLevels.Levels)
                        {
                            if (detail.Selectable)
                            {
                                levels.Add(id);
                            }
                        }
                    }

                    currentLevel = alertComp.CurrentLevel;
                    currentDelay = _alertLevelSystem.GetAlertLevelDelay(stationUid.Value, alertComp);
                }

                if (TryComp(stationUid.Value, out ERTCallComponent? ertComponent) && ertComponent.ERTTeams != null)
                {
                    ertList = new();
                    foreach (var (id, detail) in ertComponent.ERTTeams.Teams)
                    {
                        ertList.Add(id);
                    }
                }
            }

            _uiSystem.SetUiState(uid, CommunicationsConsoleUiKey.Key, new CommunicationsConsoleInterfaceState(
                    CanAnnounce(comp),
                    CanCallOrRecall(comp),
                    levels,
                    currentLevel,
                    currentDelay,
                    _roundEndSystem.ExpectedCountdownEnd,
                    CanCallOrRecallERT(comp),
                    ertList,
                    _ertCallSystem.TimeToErt(stationUid),
                    FirstPrivilegedIdIsPresented(comp),
                    SecondPrivilegedIdIsPresented(comp),
                    FirstPrivilegedIdIsValid(comp),
                    SecondPrivilegedIdIsValid(comp)
                ));
        }

        private bool FirstPrivilegedIdIsPresented(CommunicationsConsoleComponent comp)
        {
            return comp.FirstPrivilegedIdSlot.Item is { Valid: true };
        }

        private bool SecondPrivilegedIdIsPresented(CommunicationsConsoleComponent comp)
        {
            return comp.SecondPrivilegedIdSlot.Item is { Valid: true };
        }

        private bool FirstPrivilegedIdIsValid(CommunicationsConsoleComponent comp)
        {
            if (EntityManager.TryGetComponent<AccessComponent>(comp.FirstPrivilegedIdSlot.Item, out var accesses))
            {
                foreach (var access in accesses.Tags.ToArray())
                {
                    if (access == comp.FirstPrivilegedIdTargetAccess)
                        return true;
                }
            }
            return false;
        }

        private bool SecondPrivilegedIdIsValid(CommunicationsConsoleComponent comp)
        {
            if (EntityManager.TryGetComponent<AccessComponent>(comp.SecondPrivilegedIdSlot.Item, out var accesses))
            {
                foreach (var access in accesses.Tags.ToArray())
                {
                    if (access == comp.SecondPrivilegedIdTargetAccess)
                        return true;
                }
            }
            return false;
        }

        private static bool CanAnnounce(CommunicationsConsoleComponent comp)
        {
            return comp.AnnouncementCooldownRemaining <= 0f;
        }

        private bool CanUse(EntityUid user, EntityUid console)
        {
            if (TryComp<AccessReaderComponent>(console, out var accessReaderComponent))
            {
                return _accessReaderSystem.IsAllowed(user, console, accessReaderComponent);
            }
            return true;
        }

        private bool CanCallOrRecall(CommunicationsConsoleComponent comp)
        {
            // Defer to what the round end system thinks we should be able to do.
            if (_emergency.EmergencyShuttleArrived || !_roundEndSystem.CanCallOrRecall())
                return false;

            // Calling shuttle checks
            if (_roundEndSystem.ExpectedCountdownEnd is null)
                return comp.CanCallShuttle;

            // Recalling shuttle checks
            var recallThreshold = _cfg.GetCVar(CCVars.EmergencyRecallTurningPoint);

            // shouldn't really be happening if we got here
            if (_roundEndSystem.ShuttleTimeLeft is not { } left
                || _roundEndSystem.ExpectedShuttleLength is not { } expected)
                return false;

            return !(left.TotalSeconds / expected.TotalSeconds < recallThreshold);
        }

        private bool CanCallOrRecallERT(CommunicationsConsoleComponent comp)
        {
            if (!comp.CanCallERT)
                return false;

            if (comp.CallERTCooldownRemaining <= 0f && FirstPrivilegedIdIsValid(comp) && SecondPrivilegedIdIsValid(comp))
            {
                return true;
            }

            return false;
        }

        private void OnSetAlertLevelMessage(EntityUid uid, CommunicationsConsoleComponent comp, CommunicationsConsoleSetAlertLevelMessage message)
        {
            if (message.Actor is not { Valid: true } mob)
                return;

            if (!CanUse(mob, uid))
            {
                _popupSystem.PopupEntity(Loc.GetString("comms-console-permission-denied"), uid, message.Actor);
                return;
            }

            var stationUid = _stationSystem.GetOwningStation(uid);
            if (stationUid != null)
            {
                _alertLevelSystem.SetLevel(stationUid.Value, message.Level, true, true);
                _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(mob):player} has set alert level {message.Level}.");
            }
        }

        private void OnSelectAlertLevelMessage(EntityUid uid, CommunicationsConsoleComponent comp, CommunicationsConsoleSelectAlertLevelMessage message)
        {
            UpdateCommsConsoleInterface();
        }

        private void OnAnnounceMessage(EntityUid uid, CommunicationsConsoleComponent comp,
            CommunicationsConsoleAnnounceMessage message)
        {
            var maxLength = _cfg.GetCVar(CCVars.ChatMaxAnnouncementLength);
            var msg = SharedChatSystem.SanitizeAnnouncement(message.Message, maxLength);
            string originalMessage = msg;
            var author = Loc.GetString("comms-console-announcement-unknown-sender");
            if (message.Actor is { Valid: true } mob)
            {
                if (!CanAnnounce(comp))
                {
                    return;
                }

                if (!CanUse(mob, uid))
                {
                    _popupSystem.PopupEntity(Loc.GetString("comms-console-permission-denied"), uid, message.Actor);
                    return;
                }

                var tryGetIdentityShortInfoEvent = new TryGetIdentityShortInfoEvent(uid, mob);
                RaiseLocalEvent(tryGetIdentityShortInfoEvent);
                author = tryGetIdentityShortInfoEvent.Title;
            }

            comp.AnnouncementCooldownRemaining = comp.DelayBetweenAnnouncements;
            UpdateCommsConsoleInterface(uid, comp);

            var ev = new CommunicationConsoleAnnouncementEvent(uid, comp, msg, message.Actor);
            RaiseLocalEvent(ref ev);

            // allow admemes with vv
            Loc.TryGetString(comp.Title, out var title);
            title ??= comp.Title;

            msg += "\n" + Loc.GetString("comms-console-announcement-sent-by") + " " + author;
            if (comp.Global)
            {
                _chatSystem.DispatchGlobalAnnouncement(msg, title, announcementSound: comp.AnnouncementSound, colorOverride: comp.Color, originalMessage: originalMessage, author: message.Actor);

                _adminLogger.Add(LogType.Chat, LogImpact.Low, $"{ToPrettyString(message.Actor):player} has sent the following global announcement: {msg}");
                return;
            }

            _chatSystem.DispatchGlobalAnnouncement(msg, title, announcementSound: comp.AnnouncementSound, colorOverride: comp.Color, originalMessage: originalMessage, author: message.Actor); // DS14 TODO

            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"{ToPrettyString(message.Actor):player} has sent the following global announcement: {msg}"); // DS14 TODO (has sent the following station announcement)

        }

        private void OnBroadcastMessage(EntityUid uid, CommunicationsConsoleComponent component, CommunicationsConsoleBroadcastMessage message)
        {
            if (!TryComp<DeviceNetworkComponent>(uid, out var net))
                return;

            var payload = new NetworkPayload
            {
                [ScreenMasks.Text] = message.Message
            };

            _deviceNetworkSystem.QueuePacket(uid, null, payload, net.TransmitFrequency);

            _adminLogger.Add(LogType.DeviceNetwork, LogImpact.Low, $"{ToPrettyString(message.Actor):player} has sent the following broadcast: {message.Message:msg}");
        }

        private void OnCallShuttleMessage(EntityUid uid, CommunicationsConsoleComponent comp, CommunicationsConsoleCallEmergencyShuttleMessage message)
        {
            if (!CanCallOrRecall(comp))
                return;

            var mob = message.Actor;

            if (!CanUse(mob, uid))
            {
                _popupSystem.PopupEntity(Loc.GetString("comms-console-permission-denied"), uid, message.Actor);
                return;
            }

            var ev = new CommunicationConsoleCallShuttleAttemptEvent(uid, comp, mob);
            RaiseLocalEvent(ref ev);
            if (ev.Cancelled)
            {
                _popupSystem.PopupEntity(ev.Reason ?? Loc.GetString("comms-console-shuttle-unavailable"), uid, message.Actor);
                return;
            }

            _roundEndSystem.RequestRoundEnd(uid);
            _adminLogger.Add(LogType.Action, LogImpact.High, $"{ToPrettyString(mob):player} has called the shuttle.");
        }

        private void OnRecallShuttleMessage(EntityUid uid, CommunicationsConsoleComponent comp, CommunicationsConsoleRecallEmergencyShuttleMessage message)
        {
            if (!CanCallOrRecall(comp))
                return;

            if (!CanUse(message.Actor, uid))
            {
                _popupSystem.PopupEntity(Loc.GetString("comms-console-permission-denied"), uid, message.Actor);
                return;
            }

            _roundEndSystem.CancelRoundEndCountdown(uid);
            _adminLogger.Add(LogType.Action, LogImpact.High, $"{ToPrettyString(message.Actor):player} has recalled the shuttle.");
        }

        private void OnERTCallMessage(EntityUid uid, CommunicationsConsoleComponent comp, CommunicationsConsoleCallERTMessage message)
        {
            if (!CanCallOrRecallERT(comp))
            {
                _popupSystem.PopupEntity(Loc.GetString("comms-console-permission-denied"), uid, message.Actor);
                return;
            }

            if (message.Actor is not { Valid: true } mob) return;

            var stationUid = _stationSystem.GetOwningStation(uid);
            if (stationUid == null)
                return;

            var mes = message.Message;

            if (mes == null)
                mes = "No reason";

            _chatManager.SendAdminAlert(Loc.GetString("comms-console-menu-ert-message-alert", ("name", mob), ("ert", Loc.GetString($"ert-team-name-{message.ERTTeam}")), ("message", mes)));

            _ertCallSystem.CallErt(stationUid.Value, message.ERTTeam);
            comp.CallERTCooldownRemaining = comp.DelayBetweenERTCall;
            UpdateCommsConsoleInterface();

            _adminLogger.Add(LogType.Action, LogImpact.Extreme, $"{ToPrettyString(mob):player} has call ERT {message.ERTTeam} with reason {message.Message}.");
        }

        private void OnERTRecallMessage(EntityUid uid, CommunicationsConsoleComponent comp, CommunicationsConsoleRecallERTMessage message)
        {
            if (!CanCallOrRecallERT(comp))
            {
                _popupSystem.PopupEntity(Loc.GetString("comms-console-permission-denied"), uid, message.Actor);
                return;
            }

            if (message.Actor is not { Valid: true } mob) return;

            var stationUid = _stationSystem.GetOwningStation(uid);
            if (stationUid == null)
                return;

            if (!_ertCallSystem.RecallERT(stationUid.Value))
            {
                _popupSystem.PopupEntity(Loc.GetString("comms-console-call-ert-fall"), uid, message.Actor);
                return;
            }

            comp.CallERTCooldownRemaining = comp.DelayBetweenERTCall;
            UpdateCommsConsoleInterface();

            _adminLogger.Add(LogType.Action, LogImpact.Extreme, $"{ToPrettyString(mob):player} has recall ERT.");
        }

        private void OnERTSelectMessage(EntityUid uid, CommunicationsConsoleComponent comp, CommunicationsConsoleSelectERTMessage message)
        {
            UpdateCommsConsoleInterface();
        }
    }

    /// <summary>
    /// Raised on announcement
    /// </summary>
    [ByRefEvent]
    public record struct CommunicationConsoleAnnouncementEvent(EntityUid Uid, CommunicationsConsoleComponent Component, string Text, EntityUid? Sender)
    {
        public EntityUid Uid = Uid;
        public CommunicationsConsoleComponent Component = Component;
        public EntityUid? Sender = Sender;
        public string Text = Text;
    }

    /// <summary>
    /// Raised on shuttle call attempt. Can be cancelled
    /// </summary>
    [ByRefEvent]
    public record struct CommunicationConsoleCallShuttleAttemptEvent(EntityUid Uid, CommunicationsConsoleComponent Component, EntityUid? Sender)
    {
        public bool Cancelled = false;
        public EntityUid Uid = Uid;
        public CommunicationsConsoleComponent Component = Component;
        public EntityUid? Sender = Sender;
        public string? Reason;
    }
}
