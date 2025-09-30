using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Shared.GameTicking;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;
using System.Linq;
using Content.Shared.GameTicking.Components;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// GameRule system that automatically restarts the round after a delay when the round ends.
/// </summary>
public sealed class AutoRoundEndingRuleSystem : GameRuleSystem<AutoRoundEndingRuleComponent>
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;

    private float _restartDelay = 15f; // seconds
    private TimeSpan? _roundEndTime;
    private bool _roundEnded;
    private bool _notified;

    private static readonly ISawmill Sawmill = Logger.GetSawmill("auto-restart");

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChanged);
    }

    private void OnRunLevelChanged(GameRunLevelChangedEvent ev)
    {
        Sawmill.Info($"[AutoRoundRestart] RunLevelChanged: {ev.New}");
        if (!EntityQuery<AutoRoundRestartRuleComponent>().Any())
            return;

        if (ev.New == GameRunLevel.PostRound)
        {
            OnRoundEnd();
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_roundEnded || _roundEndTime == null)
            return;

        var timeSinceEnd = _gameTiming.CurTime - _roundEndTime.Value;
        var secondsLeft = _restartDelay - (float)timeSinceEnd.TotalSeconds;

        if (!_notified && secondsLeft <= 5f && secondsLeft > 0f)
        {
            NotifyPlayers($"Авиаудар нанесен. Конец боя через: {MathF.Ceiling(secondsLeft)} секунд!");
            _notified = true;
        }

        if (timeSinceEnd.TotalSeconds >= _restartDelay)
        {
            RestartRound();
            _roundEnded = false;
            _roundEndTime = null;
            _notified = false;
        }
    }

    private void OnRoundEnd()
    {
        _roundEnded = true;
        _roundEndTime = _gameTiming.CurTime;
        _notified = false;
        NotifyPlayers($"Сосредоточенный авиаудар через: {_restartDelay} секунд.");
    }

    private void NotifyPlayers(string message)
    {
        _chatSystem.DispatchGlobalAnnouncement(message, sender: "Мировая арена");
        Sawmill.Info($"[AutoRoundRestart] {message}");
    }

    private void RestartRound()
    {
        _gameTicker.RestartRound();
        Sawmill.Info("[AutoRoundRestart] Restarting round now!");
    }
}