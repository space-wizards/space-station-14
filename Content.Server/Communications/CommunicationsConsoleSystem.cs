using System.Globalization;
using System.Threading;
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
using Timer = Robust.Shared.Timing.Timer;

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

        private TimeSpan _delayBetweenShuttleCalls = TimeSpan.FromSeconds(120);
        private TimeSpan _shuttleStatusLastChanged = TimeSpan.Zero;

        public override void Initialize()
        {
            SubscribeLocalEvent<CommunicationsConsoleComponent, RoundEndSystemChangedEvent>((_, comp, _) => UpdateBoundUserInterface(comp));
            SubscribeLocalEvent<CommunicationsConsoleComponent, AlertLevelChangedEvent>((_, comp, _) => UpdateBoundUserInterface(comp));
            SubscribeLocalEvent<CommunicationsConsoleComponent, AlertLevelDelayFinishedEvent>((_, comp, _) => UpdateBoundUserInterface(comp));

            SubscribeLocalEvent<CommunicationsConsoleComponent, CommunicationsConsoleSelectAlertLevelMessage>((uid, _, args) => OnSelectAlertLevelMessage(uid, args));
            SubscribeLocalEvent<CommunicationsConsoleComponent, CommunicationsConsoleAnnounceMessage>(OnAnnounceMessage);
            SubscribeLocalEvent<CommunicationsConsoleComponent, CommunicationsConsoleCallEmergencyShuttleMessage>((uid, _, args) => OnCallShuttleMessage(uid, args));
            SubscribeLocalEvent<CommunicationsConsoleComponent, CommunicationsConsoleRecallEmergencyShuttleMessage>((uid, _, args) => OnRecallShuttleMessage(uid, args));
        }

        public void UpdateBoundUserInterface(CommunicationsConsoleComponent comp)
        {
            var uid = comp.Owner;

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
                    _roundEndSystem.CanCall(),
                    levels,
                    currentLevel,
                    currentDelay,
                    _roundEndSystem.ExpectedCountdownEnd
                    )
                );
        }

        private bool CanAnnounce(CommunicationsConsoleComponent comp)
        {
            if (comp.LastAnnouncementTime == TimeSpan.Zero)
            {
                return true;
            }
            return _gameTiming.CurTime >= comp.LastAnnouncementTime + TimeSpan.FromSeconds(comp.DelayBetweenAnnouncements);
        }

        private void OnSelectAlertLevelMessage(EntityUid uid, CommunicationsConsoleSelectAlertLevelMessage message)
        {
            var stationUid = _stationSystem.GetOwningStation(uid);
            if (stationUid != null)
            {
                _alertLevelSystem.SetLevel(stationUid.Value, message.Level, true, true);
            }
        }

        private void OnAnnounceMessage(EntityUid uid, CommunicationsConsoleComponent comp, CommunicationsConsoleAnnounceMessage message)
        {
            if (!CanAnnounce(comp))
            {
                return;
            }

            comp.LastAnnouncementTime = _gameTiming.CurTime;
            UpdateBoundUserInterface(comp);

            var msg = message.Message.Length <= MaxMessageLength ? message.Message.Trim() : $"{message.Message.Trim().Substring(0, MaxMessageLength)}...";
            var author = Loc.GetString("communicationsconsole-announcement-unknown-sender");
            if (message.Session.AttachedEntity is {Valid: true} mob && _idCardSystem.TryFindIdCard(mob, out var id))
            {
                if (_entityManager.TryGetComponent<AccessReaderComponent>(uid, out var accessReaderComponent) && accessReaderComponent.Enabled)
                {
                    if (!_accessReaderSystem.IsAllowed(accessReaderComponent, mob))
                    {
                        _popupSystem.PopupEntity(Loc.GetString("communicationsconsole-permission-denied"), uid, Filter.Entities(mob));
                        return;
                    }
                }

                author = $"{id.FullName} ({CultureInfo.CurrentCulture.TextInfo.ToTitleCase(id.JobTitle ?? string.Empty)})".Trim();
            }

            msg += "\n" + Loc.GetString("communicationsconsole-announcement-sent-by") + " " + author;
            _chatManager.DispatchStationAnnouncement(msg, Loc.GetString(comp.AnnouncementDisplayName));
        }

        private void OnCallShuttleMessage(EntityUid uid, CommunicationsConsoleCallEmergencyShuttleMessage message)
        {
            _roundEndSystem.RequestRoundEnd(uid);
        }

        private void OnRecallShuttleMessage(EntityUid uid, CommunicationsConsoleRecallEmergencyShuttleMessage message)
        {
            _roundEndSystem.CancelRoundEndCountdown(uid);
        }
    }
}
