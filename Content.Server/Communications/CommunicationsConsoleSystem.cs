using System.Globalization;
using Content.Server.Access.Systems;
using Content.Server.AlertLevel;
using Content.Server.Chat.Managers;
using Content.Server.Popups;
using Content.Server.RoundEnd;
using Content.Server.Station.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Communications;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Communications
{
    public sealed class CommunicationsConsoleSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
        [Dependency] private readonly AlertLevelSystem _alertLevelSystem = default!;
        [Dependency] private readonly StationSystem _stationSystem = default!;
        [Dependency] private readonly IdCardSystem _idCardSystem = default!;
        [Dependency] private readonly AccessReaderSystem _accessReaderSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;

        private const int MaxMessageLength = 256;

        public override void Initialize()
        {
            // All events that refresh the BUI
            SubscribeLocalEvent<CommunicationsConsoleComponent, ComponentInit>((uid, comp, _) => UpdateBoundUserInterface(uid, comp));
            SubscribeLocalEvent<CommunicationsConsoleComponent, RoundEndSystemChangedEvent>((uid, comp, _) => UpdateBoundUserInterface(uid, comp));
            SubscribeLocalEvent<CommunicationsConsoleComponent, AlertLevelChangedEvent>((uid, comp, _) => UpdateBoundUserInterface(uid, comp));
            SubscribeLocalEvent<CommunicationsConsoleComponent, AlertLevelDelayFinishedEvent>((uid, comp, _) => UpdateBoundUserInterface(uid, comp));

            // Messages from the BUI
            SubscribeLocalEvent<CommunicationsConsoleComponent, CommunicationsConsoleSelectAlertLevelMessage>(OnSelectAlertLevelMessage);
            SubscribeLocalEvent<CommunicationsConsoleComponent, CommunicationsConsoleAnnounceMessage>(OnAnnounceMessage);
            SubscribeLocalEvent<CommunicationsConsoleComponent, CommunicationsConsoleCallEmergencyShuttleMessage>(OnCallShuttleMessage);
            SubscribeLocalEvent<CommunicationsConsoleComponent, CommunicationsConsoleRecallEmergencyShuttleMessage>(OnRecallShuttleMessage);
        }

        public override void Update(float frameTime)
        {
            foreach (var comp in EntityManager.EntityQuery<CommunicationsConsoleComponent>())
            {
                if (comp.AnnouncementCooldownRemaining <= 0f)
                {
                    // TODO: Find a less ass way of refreshing the UI
                    if (!comp.AlreadyRefreshed) return;
                    UpdateBoundUserInterface(comp.Owner, comp);
                    comp.AlreadyRefreshed = true;
                    return;
                }
                comp.AnnouncementCooldownRemaining -= frameTime;
            }

            base.Update(frameTime);
        }

        private void UpdateBoundUserInterface(EntityUid uid, CommunicationsConsoleComponent comp)
        {
            var stationUid = _stationSystem.GetOwningStation(uid);
            List<string>? levels = null;
            string currentLevel = default!;
            float currentDelay = 0;

            if (stationUid != null)
            {
                if (_entityManager.TryGetComponent(stationUid.Value, out AlertLevelComponent alertComp) &&
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
                    CanCall(comp),
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
            if (_entityManager.TryGetComponent<AccessReaderComponent>(console, out var accessReaderComponent) && accessReaderComponent.Enabled)
            {
                return _accessReaderSystem.IsAllowed(accessReaderComponent, user);
            }
            return true;
        }

        private bool CanCall(CommunicationsConsoleComponent comp)
        {
            return comp.CanCallShuttle && _roundEndSystem.CanCall();
        }

        private void OnSelectAlertLevelMessage(EntityUid uid, CommunicationsConsoleComponent comp, CommunicationsConsoleSelectAlertLevelMessage message)
        {
            if (message.Session.AttachedEntity is not {Valid: true} mob) return;
            if (!CanUse(mob, uid)) return;

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
            var author = Loc.GetString("communicationsconsole-announcement-unknown-sender");
            if (message.Session.AttachedEntity is {Valid: true} mob)
            {
                if (!CanAnnounce(comp))
                {
                    return;
                }

                if (_idCardSystem.TryFindIdCard(mob, out var id))
                {
                    author = $"{id.FullName} ({CultureInfo.CurrentCulture.TextInfo.ToTitleCase(id.JobTitle ?? string.Empty)})".Trim();
                }

                if (!CanUse(mob, uid))
                {
                    _popupSystem.PopupEntity(Loc.GetString("communicationsconsole-permission-denied"), uid, Filter.Entities(mob));
                    return;
                }
            }

            comp.AnnouncementCooldownRemaining = comp.DelayBetweenAnnouncements;
            comp.AlreadyRefreshed = false;
            UpdateBoundUserInterface(uid, comp);

            msg += "\n" + Loc.GetString("communicationsconsole-announcement-sent-by") + " " + author;
            _chatManager.DispatchStationAnnouncement(msg, Loc.GetString(comp.AnnouncementDisplayName), colorOverride: comp.AnnouncementColor);
        }

        private void OnCallShuttleMessage(EntityUid uid, CommunicationsConsoleComponent comp, CommunicationsConsoleCallEmergencyShuttleMessage message)
        {
            if (!comp.CanCallShuttle) return;
            if (message.Session.AttachedEntity is not {Valid: true} mob) return;
            if (CanUse(mob, uid))
            {
                _roundEndSystem.RequestRoundEnd(uid);
            }
            else
            {
                _popupSystem.PopupEntity(Loc.GetString("communicationsconsole-permission-denied"), uid, Filter.Entities(mob));
            }
        }

        private void OnRecallShuttleMessage(EntityUid uid, CommunicationsConsoleComponent comp, CommunicationsConsoleRecallEmergencyShuttleMessage message)
        {
            if (!comp.CanCallShuttle) return;
            if (message.Session.AttachedEntity is not {Valid: true} mob) return;
            if (CanUse(mob, uid))
            {
                _roundEndSystem.CancelRoundEndCountdown(uid);
            }
            else
            {
                _popupSystem.PopupEntity(Loc.GetString("communicationsconsole-permission-denied"), uid, Filter.Entities(mob));
            }
        }
    }
}
