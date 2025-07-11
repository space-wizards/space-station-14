using System.Threading;
using Content.Server.Administration.Logs;
using Content.Server.AlertLevel;
using Content.Shared.CCVar;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.GameTicking;
using Content.Server.Screens.Components;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Database;
using Content.Shared.DeviceNetwork;
using Content.Shared.GameTicking;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Shared.DeviceNetwork.Components;
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
        [Dependency] private readonly DeviceNetworkSystem _deviceNetworkSystem = default!;
        [Dependency] private readonly EmergencyShuttleSystem _shuttle = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
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
        private bool _autoCalledBefore = false;

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
            _autoCalledBefore = false;
            RaiseLocalEvent(RoundEndSystemChangedEvent.Default);
        }

        /// <summary>
        ///     Attempts to get the MapUid of the station using <see cref="StationSystem.GetLargestGrid"/>
        /// </summary>
        public EntityUid? GetStation()
        {
            AllEntityQuery<StationEmergencyShuttleComponent, StationDataComponent>().MoveNext(out _, out _, out var data);
            if (data == null)
                return null;
            var targetGrid = _stationSystem.GetLargestGrid(data);
            return targetGrid == null ? null : Transform(targetGrid.Value).MapUid;
        }

        /// <summary>
        ///     Attempts to get centcomm's MapUid
        /// </summary>
        public EntityUid? GetCentcomm()
        {
            AllEntityQuery<StationCentcommComponent>().MoveNext(out var centcomm);

            return centcomm == null ? null : centcomm.MapEntity;
        }

        public bool CanCallOrRecall()
        {
            return _cooldownTokenSource == null;
        }

        public bool IsRoundEndRequested()
        {
            return _countdownTokenSource != null;
        }

        public void RequestRoundEnd(EntityUid? requester = null, bool checkCooldown = true, string text = "round-end-system-shuttle-called-announcement", string name = "round-end-system-shuttle-sender-announcement")
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

            RequestRoundEnd(duration, requester, checkCooldown, text, name);
        }

        public void RequestRoundEnd(TimeSpan countdownTime, EntityUid? requester = null, bool checkCooldown = true, string text = "round-end-system-shuttle-called-announcement", string name = "round-end-system-shuttle-sender-announcement")
        {
            if (_gameTicker.RunLevel != GameRunLevel.InRound)
                return;

            if (checkCooldown && _cooldownTokenSource != null)
                return;

            if (_countdownTokenSource != null)
                return;

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

            _chatSystem.DispatchGlobalAnnouncement(Loc.GetString(text,
                ("time", time),
                ("units", Loc.GetString(units))),
                Loc.GetString(name),
                false,
                null,
                Color.Gold);

            _audio.PlayGlobal("/Audio/Announcements/shuttlecalled.ogg", Filter.Broadcast(), true);

            LastCountdownStart = _gameTiming.CurTime;
            ExpectedCountdownEnd = _gameTiming.CurTime + countdownTime;

            // TODO full game saves
            Timer.Spawn(countdownTime, _shuttle.DockEmergencyShuttle, _countdownTokenSource.Token);

            ActivateCooldown();
            RaiseLocalEvent(RoundEndSystemChangedEvent.Default);

            var shuttle = _shuttle.GetShuttle();
            if (shuttle != null && TryComp<DeviceNetworkComponent>(shuttle, out var net))
            {
                var payload = new NetworkPayload
                {
                    [ShuttleTimerMasks.ShuttleMap] = shuttle,
                    [ShuttleTimerMasks.SourceMap] = GetCentcomm(),
                    [ShuttleTimerMasks.DestMap] = GetStation(),
                    [ShuttleTimerMasks.ShuttleTime] = countdownTime,
                    [ShuttleTimerMasks.SourceTime] = countdownTime + TimeSpan.FromSeconds(_shuttle.TransitTime + _cfg.GetCVar(CCVars.EmergencyShuttleDockTime)),
                    [ShuttleTimerMasks.DestTime] = countdownTime,
                };
                _deviceNetworkSystem.QueuePacket(shuttle.Value, null, payload, net.TransmitFrequency);
            }
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
                Loc.GetString("round-end-system-shuttle-sender-announcement"), false, colorOverride: Color.Gold);

            _audio.PlayGlobal("/Audio/Announcements/shuttlerecalled.ogg", Filter.Broadcast(), true);

            LastCountdownStart = null;
            ExpectedCountdownEnd = null;
            ActivateCooldown();
            RaiseLocalEvent(RoundEndSystemChangedEvent.Default);

            // remove active clientside evac shuttle timers by zeroing the target time
            var zero = TimeSpan.Zero;
            var shuttle = _shuttle.GetShuttle();
            if (shuttle != null && TryComp<DeviceNetworkComponent>(shuttle, out var net))
            {
                var payload = new NetworkPayload
                {
                    [ShuttleTimerMasks.ShuttleMap] = shuttle,
                    [ShuttleTimerMasks.SourceMap] = GetCentcomm(),
                    [ShuttleTimerMasks.DestMap] = GetStation(),
                    [ShuttleTimerMasks.ShuttleTime] = zero,
                    [ShuttleTimerMasks.SourceTime] = zero,
                    [ShuttleTimerMasks.DestTime] = zero,
                };
                _deviceNetworkSystem.QueuePacket(shuttle.Value, null, payload, net.TransmitFrequency);
            }
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

        /// <summary>
        /// Starts a behavior to end the round
        /// </summary>
        /// <param name="behavior">The way in which the round will end</param>
        /// <param name="time"></param>
        /// <param name="sender"></param>
        /// <param name="textCall"></param>
        /// <param name="textAnnounce"></param>
        public void DoRoundEndBehavior(RoundEndBehavior behavior,
            TimeSpan time,
            string sender = "comms-console-announcement-title-centcom",
            string textCall = "round-end-system-shuttle-called-announcement",
            string textAnnounce = "round-end-system-shuttle-already-called-announcement")
        {
            switch (behavior)
            {
                case RoundEndBehavior.InstantEnd:
                    EndRound();
                    break;
                case RoundEndBehavior.ShuttleCall:
                    // Check is shuttle called or not. We should only dispatch announcement if it's already called
                    if (IsRoundEndRequested())
                    {
                        _chatSystem.DispatchGlobalAnnouncement(Loc.GetString(textAnnounce),
                            Loc.GetString(sender),
                            colorOverride: Color.Gold);
                    }
                    else
                    {
                        RequestRoundEnd(time, null, false, textCall,
                            Loc.GetString(sender));
                    }
                    break;
            }
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

            // TODO full game saves
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
            int mins = _autoCalledBefore ? _cfg.GetCVar(CCVars.EmergencyShuttleAutoCallExtensionTime)
                                        : _cfg.GetCVar(CCVars.EmergencyShuttleAutoCallTime);
            if (mins != 0 && _gameTiming.CurTime - AutoCallStartTime > TimeSpan.FromMinutes(mins))
            {
                if (!_shuttle.EmergencyShuttleArrived && ExpectedCountdownEnd is null)
                {
                    RequestRoundEnd(null, false, "round-end-system-shuttle-auto-called-announcement");
                    _autoCalledBefore = true;
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

    public enum RoundEndBehavior : byte
    {
        /// <summary>
        /// Instantly end round
        /// </summary>
        InstantEnd,

        /// <summary>
        /// Call shuttle with custom announcement
        /// </summary>
        ShuttleCall,

        /// <summary>
        /// Do nothing
        /// </summary>
        Nothing
    }
}
