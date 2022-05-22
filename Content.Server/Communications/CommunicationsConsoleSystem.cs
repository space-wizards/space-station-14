using Content.Server.AlertLevel;
using Content.Server.Chat.Managers;
using Content.Server.RoundEnd;
using Content.Server.Station.Systems;
using Content.Shared.Communications;
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

        private TimeSpan _delayBetweenShuttleCalls = TimeSpan.FromSeconds(120);
        private TimeSpan _shuttleStatusLastChanged = TimeSpan.Zero;

        public override void Initialize()
        {
            SubscribeLocalEvent<CommunicationsConsoleComponent, RoundEndSystemChangedEvent>((uid, comp, _) => UpdateBoundUserInterface(uid, comp));
            SubscribeLocalEvent<CommunicationsConsoleComponent, AlertLevelChangedEvent>((uid, comp, _) => UpdateBoundUserInterface(uid, comp));
            SubscribeLocalEvent<CommunicationsConsoleComponent, AlertLevelDelayFinishedEvent>((uid, comp, _) => UpdateBoundUserInterface(uid, comp));

            SubscribeLocalEvent<CommunicationsConsoleComponent, CommunicationsConsoleSelectAlertLevelMessage>();
            SubscribeLocalEvent<CommunicationsConsoleComponent, CommunicationsConsoleAnnounceMessage>();
            SubscribeLocalEvent<CommunicationsConsoleComponent, CommunicationsConsoleCallEmergencyShuttleMessage>();
            SubscribeLocalEvent<CommunicationsConsoleComponent, CommunicationsConsoleRecallEmergencyShuttleMessage>();
        }

        public void UpdateBoundUserInterface(EntityUid uid, CommunicationsConsoleComponent comp)
        {
            comp.UserInterface?.SetState(
                new CommunicationsConsoleInterfaceState(
                    CanAnnounce(comp),
                    _roundEndSystem.CanCall(),
                    GetAlerts(uid),
                    GetAlertComp(uid).CurrentLevel,
                    _roundEndSystem.ExpectedCountdownEnd
                    );
                )
        }

        public bool CanAnnounce(CommunicationsConsoleComponent comp)
        {
            return _gameTiming.CurTime >= comp.LastAnnouncementTime + comp.DelayBetweenAnnouncements;
        }

        public AlertLevelComponent? GetAlertComp(EntityUid uid)
        {
            var stationUid = _stationSystem.GetOwningStation(uid);
            if (stationUid != null)
            {
                _entityManager.TryGetComponent(stationUid.Value, out AlertLevelComponent? alertComp);
                return alertComp;
            }
        }

        public List<string?> GetAlerts(EntityUid uid)
        {
            var alertComp = GetAlertComp(uid);
        }
    }
}
