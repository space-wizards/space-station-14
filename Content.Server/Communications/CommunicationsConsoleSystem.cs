using System.Globalization;
using System.Linq;
using Content.Server.Access.Systems;
using Content.Server.Administration.Logs;
using Content.Server.AlertLevel;
using Content.Server.Chat;
using Content.Server.Chat.Systems;
using Content.Server.Interaction;
using Content.Server.Popups;
using Content.Server.RoundEnd;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.CCVar;
using Content.Shared.Communications;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Server.Communications
{
    public sealed class CommunicationsConsoleSystem : EntitySystem
    {
        [Dependency] private readonly AccessReaderSystem _accessReaderSystem = default!;
        [Dependency] private readonly InteractionSystem _interaction = default!;
        [Dependency] private readonly AlertLevelSystem _alertLevelSystem = default!;
        [Dependency] private readonly ChatSystem _chatSystem = default!;
        [Dependency] private readonly IdCardSystem _idCardSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
        [Dependency] private readonly ShuttleSystem _shuttle = default!;
        [Dependency] private readonly StationSystem _stationSystem = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;

        private const int MaxMessageLength = 256;
        private const float UIUpdateInterval = 5.0f;

        public override void Initialize()
        {
            // All events that refresh the BUI
            SubscribeLocalEvent<AlertLevelChangedEvent>(OnAlertLevelChanged);
            SubscribeLocalEvent<CommunicationsConsoleComponent, ComponentInit>((_, comp, _) => UpdateCommsConsoleInterface(comp));
            SubscribeLocalEvent<RoundEndSystemChangedEvent>(_ => OnGenericBroadcastEvent());
            SubscribeLocalEvent<AlertLevelDelayFinishedEvent>(_ => OnGenericBroadcastEvent());

            // Messages from the BUI
            SubscribeLocalEvent<CommunicationsConsoleComponent, CommunicationsConsoleSelectAlertLevelMessage>(OnSelectAlertLevelMessage);
            SubscribeLocalEvent<CommunicationsConsoleComponent, CommunicationsConsoleAnnounceMessage>(OnAnnounceMessage);
            SubscribeLocalEvent<CommunicationsConsoleComponent, CommunicationsConsoleCallEmergencyShuttleMessage>(OnCallShuttleMessage);
            SubscribeLocalEvent<CommunicationsConsoleComponent, CommunicationsConsoleRecallEmergencyShuttleMessage>(OnRecallShuttleMessage);
        }

        public override void Update(float frameTime)
        {
            foreach (var comp in EntityQuery<CommunicationsConsoleComponent>())
            {
                // TODO refresh the UI in a less horrible way
                if (comp.AnnouncementCooldownRemaining >= 0f)
                {
                    comp.AnnouncementCooldownRemaining -= frameTime;
                }

                comp.UIUpdateAccumulator += frameTime;

                if (comp.UIUpdateAccumulator < UIUpdateInterval)
                    continue;

                comp.UIUpdateAccumulator -= UIUpdateInterval;

                if (comp.UserInterface is {} ui && ui.SubscribedSessions.Count > 0)
                    UpdateCommsConsoleInterface(comp);
            }

            base.Update(frameTime);
        }

        /// <summary>
        /// Update the UI of every comms console.
        /// </summary>
        private void OnGenericBroadcastEvent()
        {
            foreach (var comp in EntityQuery<CommunicationsConsoleComponent>())
            {
                UpdateCommsConsoleInterface(comp);
            }
        }

        /// <summary>
        /// Updates all comms consoles belonging to the station that the alert level was set on
        /// </summary>
        /// <param name="args">Alert level changed event arguments</param>
        private void OnAlertLevelChanged(AlertLevelChangedEvent args)
        {
            foreach (var comp in EntityQuery<CommunicationsConsoleComponent>(true))
            {
                var entStation = _stationSystem.GetOwningStation(comp.Owner);
                if (args.Station == entStation)
                {
                    UpdateCommsConsoleInterface(comp);
                }
            }
        }

        /// <summary>
        /// Updates the UI for all comms consoles.
        /// </summary>
        public void UpdateCommsConsoleInterface()
        {
            foreach (var comp in EntityQuery<CommunicationsConsoleComponent>())
            {
                UpdateCommsConsoleInterface(comp);
            }
        }

        /// <summary>
        /// Updates the UI for a particular comms console.
        /// </summary>
        /// <param name="comp"></param>
        public void UpdateCommsConsoleInterface(CommunicationsConsoleComponent comp)
        {
            var uid = comp.Owner;

            var stationUid = _stationSystem.GetOwningStation(uid);
            List<string>? levels = null;
            string currentLevel = default!;
            float currentDelay = 0;

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
            }

            comp.UserInterface?.SetState(
                new CommunicationsConsoleInterfaceState(
                    CanAnnounce(comp),
                    CanCallOrRecall(comp),
                    levels,
                    currentLevel,
                    currentDelay,
                    _roundEndSystem.ExpectedCountdownEnd
                    )
                );
        }

        private bool CanAnnounce(CommunicationsConsoleComponent comp)
        {
            return comp.AnnouncementCooldownRemaining <= 0f;
        }

        private bool CanUse(EntityUid user, EntityUid console)
        {
            // This shouldn't technically be possible because of BUI but don't trust client.
            if (!_interaction.InRangeUnobstructed(console, user))
                return false;

            if (TryComp<AccessReaderComponent>(console, out var accessReaderComponent) && accessReaderComponent.Enabled)
            {
                return _accessReaderSystem.IsAllowed(user, accessReaderComponent);
            }
            return true;
        }

        private bool CanCallOrRecall(CommunicationsConsoleComponent comp)
        {
            // Defer to what the round end system thinks we should be able to do.
            if (_shuttle.EmergencyShuttleArrived || !_roundEndSystem.CanCallOrRecall())
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

        private void OnSelectAlertLevelMessage(EntityUid uid, CommunicationsConsoleComponent comp, CommunicationsConsoleSelectAlertLevelMessage message)
        {
            if (message.Session.AttachedEntity is not {Valid: true} mob) return;
            if (!CanUse(mob, uid))
            {
                _popupSystem.PopupCursor(Loc.GetString("comms-console-permission-denied"), message.Session, PopupType.Medium);
                return;
            }

            var stationUid = _stationSystem.GetOwningStation(uid);
            if (stationUid != null)
            {
                _alertLevelSystem.SetLevel(stationUid.Value, message.Level, true, true);
            }
        }

        private void OnAnnounceMessage(EntityUid uid, CommunicationsConsoleComponent comp,
            CommunicationsConsoleAnnounceMessage message)
        {
            var msg = message.Message.Length <= MaxMessageLength ? message.Message.Trim() : $"{message.Message.Trim().Substring(0, MaxMessageLength)}...";
            var author = Loc.GetString("comms-console-announcement-unknown-sender");
            if (message.Session.AttachedEntity is {Valid: true} mob)
            {
                if (!CanAnnounce(comp))
                {
                    return;
                }

                if (!CanUse(mob, uid))
                {
                    _popupSystem.PopupEntity(Loc.GetString("comms-console-permission-denied"), uid, message.Session);
                    return;
                }

                if (_idCardSystem.TryFindIdCard(mob, out var id))
                {
                    author = $"{id.FullName} ({CultureInfo.CurrentCulture.TextInfo.ToTitleCase(id.JobTitle ?? string.Empty)})".Trim();
                }
            }

            comp.AnnouncementCooldownRemaining = comp.DelayBetweenAnnouncements;
            UpdateCommsConsoleInterface(comp);

            // allow admemes with vv
            Loc.TryGetString(comp.AnnouncementDisplayName, out var title);
            title ??= comp.AnnouncementDisplayName;

            msg += "\n" + Loc.GetString("comms-console-announcement-sent-by") + " " + author;
            if (comp.AnnounceGlobal)
            {
                _chatSystem.DispatchGlobalAnnouncement(msg, title, announcementSound: comp.AnnouncementSound, colorOverride: comp.AnnouncementColor);

                if (message.Session.AttachedEntity != null)
                    _adminLogger.Add(LogType.Chat, LogImpact.Low, $"{ToPrettyString(message.Session.AttachedEntity.Value):player} has sent the following global announcement: {msg}");

                return;
            }
            _chatSystem.DispatchStationAnnouncement(uid, msg, title, colorOverride: comp.AnnouncementColor);

            if (message.Session.AttachedEntity != null)
                _adminLogger.Add(LogType.Chat, LogImpact.Low, $"{ToPrettyString(message.Session.AttachedEntity.Value):player} has sent the following station announcement: {msg}");
        }

        private void OnCallShuttleMessage(EntityUid uid, CommunicationsConsoleComponent comp, CommunicationsConsoleCallEmergencyShuttleMessage message)
        {
            if (!CanCallOrRecall(comp)) return;
            if (message.Session.AttachedEntity is not {Valid: true} mob) return;
            if (!CanUse(mob, uid))
            {
                _popupSystem.PopupEntity(Loc.GetString("comms-console-permission-denied"), uid, message.Session);
                return;
            }
            _roundEndSystem.RequestRoundEnd(uid);
            _adminLogger.Add(LogType.Action, LogImpact.Extreme, $"{ToPrettyString(mob):player} has called the shuttle.");
        }

        private void OnRecallShuttleMessage(EntityUid uid, CommunicationsConsoleComponent comp, CommunicationsConsoleRecallEmergencyShuttleMessage message)
        {
            if (!CanCallOrRecall(comp)) return;
            if (message.Session.AttachedEntity is not {Valid: true} mob) return;
            if (!CanUse(mob, uid))
            {
                _popupSystem.PopupEntity(Loc.GetString("comms-console-permission-denied"), uid, message.Session);
                return;
            }

            _roundEndSystem.CancelRoundEndCountdown(uid);
            _adminLogger.Add(LogType.Action, LogImpact.Extreme, $"{ToPrettyString(mob):player} has recalled the shuttle.");
        }
    }
}
