using System.Threading;
using Content.Server.Administration.Logs;
using Content.Server.AlertLevel;
using Content.Shared.CCVar;
using Content.Server.Chat;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Systems;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.RoundEnd
{
    /// <summary>
    /// Handles ending rounds normally and also via requesting it (e.g. via comms console)
    /// If you request a round end then an escape shuttle will be used.
    /// </summary>
    public sealed class RoundEndSystem : EntitySystem
    {
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IPrototypeManager _protoManager = default!;
        [Dependency] private readonly ChatSystem _chatSystem = default!;
        [Dependency] private readonly GameTicker _gameTicker = default!;
        [Dependency] private readonly EmergencyShuttleSystem _shuttle = default!;
        [Dependency] private readonly StationSystem _stationSystem = default!;

        public TimeSpan DefaultCooldownDuration { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Countdown to use where there is no station alert countdown to be found.
        /// </summary>
        public TimeSpan DefaultCountdownDuration { get; set; } = TimeSpan.FromMinutes(10);

        private CancellationTokenSource? _countdownTokenSource = null;
        private CancellationTokenSource? _cooldownTokenSource = null;
        public TimeSpan? LastCountdownStart { get; set; } = null;
        public TimeSpan? ExpectedCountdownEnd { get; set; } = null;
        public TimeSpan? ExpectedShuttleLength => ExpectedCountdownEnd - LastCountdownStart;
        public TimeSpan? ShuttleTimeLeft => ExpectedCountdownEnd - _gameTiming.CurTime;

        public TimeSpan AutoCallStartTime;
        private bool AutoCalledBefore = false;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<RoundRestartCleanupEvent>(_ => Reset());
            SetAutoCallTime();
        }

        private void SetAutoCallTime()
        {
            AutoCallStartTime = _gameTiming.CurTime;
        }

        private void Reset()
        {
            if (_countdownTokenSource != null)
            {
                _countdownTokenSource.Cancel();
                _countdownTokenSource = null;
            }

            if (_cooldownTokenSource != null)
            {
                _cooldownTokenSource.Cancel();
                _cooldownTokenSource = null;
            }

            LastCountdownStart = null;
            ExpectedCountdownEnd = null;
            SetAutoCallTime();
            AutoCalledBefore = false;
            RaiseLocalEvent(RoundEndSystemChangedEvent.Default);
        }

        public bool CanCallOrRecall()
        {
            return _cooldownTokenSource == null;
        }

        public void RequestRoundEnd(EntityUid? requester = null, bool checkCooldown = true, bool autoCall = false)
        {
            var duration = DefaultCountdownDuration;

            if (requester != null)
            {
                var stationUid = _stationSystem.GetOwningStation(requester.Value);
                if (TryComp<AlertLevelComponent>(stationUid, out var alertLevel))
                {
                    duration = _protoManager
                        .Index<AlertLevelPrototype>(AlertLevelSystem.DefaultAlertLevelSet)
                        .Levels[alertLevel.CurrentLevel].ShuttleTime;
                }
            }

            RequestRoundEnd(duration, requester, checkCooldown, autoCall);
        }

        public void RequestRoundEnd(TimeSpan countdownTime, EntityUid? requester = null, bool checkCooldown = true, bool autoCall = false)
        {
            if (_gameTicker.RunLevel != GameRunLevel.InRound) return;

            if (checkCooldown && _cooldownTokenSource != null) return;

            if (_countdownTokenSource != null) return;
            _countdownTokenSource = new();

            if (requester != null)
            {
                _adminLogger.Add(LogType.ShuttleCalled, LogImpact.High, $"Shuttle called by {ToPrettyString(requester.Value):user}");
            }
            else
            {
                _adminLogger.Add(LogType.ShuttleCalled, LogImpact.High, $"Shuttle called");
            }

            // I originally had these set up here but somehow time gets passed as 0 to Loc so IDEK.
            int time;
            string units;

            if (countdownTime.TotalSeconds < 60)
            {
                time = countdownTime.Seconds;
                units = "eta-units-seconds";
            }
            else
            {
               time = countdownTime.Minutes;
               units = "eta-units-minutes";
            }

            if (autoCall)
            {
                _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("round-end-system-shuttle-auto-called-announcement",
                    ("time", time),
                    ("units", Loc.GetString(units))),
                    Loc.GetString("Station"),
                    false,
                    null,
                    Color.Gold);
            }
            else
            {
                _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("round-end-system-shuttle-called-announcement",
                    ("time", time),
                    ("units", Loc.GetString(units))),
                    Loc.GetString("Station"),
                    false,
                    null,
                    Color.Gold);
            }

            SoundSystem.Play("/Audio/Announcements/shuttlecalled.ogg", Filter.Broadcast());

            LastCountdownStart = _gameTiming.CurTime;
            ExpectedCountdownEnd = _gameTiming.CurTime + countdownTime;
            Timer.Spawn(countdownTime, _shuttle.CallEmergencyShuttle, _countdownTokenSource.Token);

            ActivateCooldown();
            RaiseLocalEvent(RoundEndSystemChangedEvent.Default);
        }

        public void CancelRoundEndCountdown(EntityUid? requester = null, bool checkCooldown = true)
        {
            if (_gameTicker.RunLevel != GameRunLevel.InRound) return;
            if (checkCooldown && _cooldownTokenSource != null) return;

            if (_countdownTokenSource == null) return;
            _countdownTokenSource.Cancel();
            _countdownTokenSource = null;

            if (requester != null)
            {
                _adminLogger.Add(LogType.ShuttleRecalled, LogImpact.High, $"Shuttle recalled by {ToPrettyString(requester.Value):user}");
            }
            else
            {
                _adminLogger.Add(LogType.ShuttleRecalled, LogImpact.High, $"Shuttle recalled");
            }

            _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("round-end-system-shuttle-recalled-announcement"),
                Loc.GetString("Station"), false, colorOverride: Color.Gold);

            SoundSystem.Play("/Audio/Announcements/shuttlerecalled.ogg", Filter.Broadcast());

            LastCountdownStart = null;
            ExpectedCountdownEnd = null;
            ActivateCooldown();
            RaiseLocalEvent(RoundEndSystemChangedEvent.Default);
        }

        public void EndRound(TimeSpan? countdownTime = null)
        {
            if (_gameTicker.RunLevel != GameRunLevel.InRound) return;
            LastCountdownStart = null;
            ExpectedCountdownEnd = null;
            RaiseLocalEvent(RoundEndSystemChangedEvent.Default);
            _gameTicker.EndRound();
            _countdownTokenSource?.Cancel();
            _countdownTokenSource = new();

            countdownTime ??= TimeSpan.FromSeconds(_cfg.GetCVar(CCVars.RoundRestartTime));
            int time;
            string unitsLocString;
            if (countdownTime.Value.TotalSeconds < 60)
            {
                time = countdownTime.Value.Seconds;
                unitsLocString = "eta-units-seconds";
            }
            else
            {
                time = countdownTime.Value.Minutes;
                unitsLocString = "eta-units-minutes";
            }
            _chatManager.DispatchServerAnnouncement(
                Loc.GetString(
                    "round-end-system-round-restart-eta-announcement",
                    ("time", time),
                    ("units", Loc.GetString(unitsLocString))));
            Timer.Spawn(countdownTime.Value, AfterEndRoundRestart, _countdownTokenSource.Token);
        }

        private void AfterEndRoundRestart()
        {
            if (_gameTicker.RunLevel != GameRunLevel.PostRound) return;
            Reset();
            _gameTicker.RestartRound();
        }

        private void ActivateCooldown()
        {
            _cooldownTokenSource?.Cancel();
            _cooldownTokenSource = new();
            Timer.Spawn(DefaultCooldownDuration, () =>
            {
                _cooldownTokenSource.Cancel();
                _cooldownTokenSource = null;
                RaiseLocalEvent(RoundEndSystemChangedEvent.Default);
            }, _cooldownTokenSource.Token);
        }

        public override void Update(float frameTime)
        {
            // Check if we should auto-call.
            int mins = AutoCalledBefore ? _cfg.GetCVar(CCVars.EmergencyShuttleAutoCallExtensionTime)
                                        : _cfg.GetCVar(CCVars.EmergencyShuttleAutoCallTime);
            if (mins != 0 && _gameTiming.CurTime - AutoCallStartTime > TimeSpan.FromMinutes(mins))
            {
                if (!_shuttle.EmergencyShuttleArrived && ExpectedCountdownEnd is null)
                {
                    RequestRoundEnd(null, false, true);
                    AutoCalledBefore = true;
                }

                // Always reset auto-call in case of a recall.
                SetAutoCallTime();
            }
        }
    }

    public sealed class RoundEndSystemChangedEvent : EntityEventArgs
    {
        public static RoundEndSystemChangedEvent Default { get; } = new();
    }
}
