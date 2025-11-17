using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Timing;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// GameRule System: restarts the round after a configured delay once the round has ended (PostRound).
/// </summary>
public sealed class AutoRoundRestartRuleSystem : GameRuleSystem<AutoRoundRestartRuleComponent>
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;

    private TimeSpan? _roundEndTime;
    private bool _postRoundActive;
    private bool _notified;

    private static readonly ISawmill Sawmill = Logger.GetSawmill("auto-restart-rule");

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChanged);
    }

    private void OnRunLevelChanged(GameRunLevelChangedEvent ev)
    {
        if (!EntityQuery<AutoRoundRestartRuleComponent, ActiveGameRuleComponent>().Any())
            return;

        switch (ev.New)
        {
            case GameRunLevel.PostRound:
                _postRoundActive = true;
                _roundEndTime = _gameTiming.CurTime;
                _notified = false;
                Sawmill.Info("[AutoRoundRestartRule] Entered PostRound. Starting timer.");
                break;
            default:
                _postRoundActive = false;
                _roundEndTime = null;
                _notified = false;
                break;
        }
    }

    public override void Update(float frameTime) 
    {
        base.Update(frameTime);

        if (!_postRoundActive || _roundEndTime is null)
            return;

        var cfg = EntityQuery<AutoRoundRestartRuleComponent, ActiveGameRuleComponent>().Select(t => t.Item1).FirstOrDefault();
        if (cfg == null)
            return;

        var timeSinceEnd = _gameTiming.CurTime - _roundEndTime.Value;
        var secondsLeft = cfg.PostRoundDelay - (float) timeSinceEnd.TotalSeconds;

        if (!_notified && secondsLeft <= cfg.PostRoundWarnThreshold && secondsLeft > 0f)
        {
            NotifyPlayers(FormatRemaining(cfg.WarnMessage, secondsLeft), cfg.SenderName);
            _notified = true;
        }

        if (timeSinceEnd.TotalSeconds >= cfg.PostRoundDelay)
        {
            if (!string.IsNullOrWhiteSpace(cfg.RestartMessage))
                NotifyPlayers(cfg.RestartMessage, cfg.SenderName);
            _gameTicker.RestartRound();
            _postRoundActive = false;
            _roundEndTime = null;
            _notified = false;
        }
    }

    private void NotifyPlayers(string message, string sender)
    {
        _chatSystem.DispatchGlobalAnnouncement(message, sender: sender);
        Sawmill.Info($"[AutoRoundRestartRule] {message}");
    }

    private static string FormatRemaining(string template, float remaining)
    {
        var rem = MathF.Ceiling(MathF.Max(0f, remaining));
        return template.Replace("{remaining}", rem.ToString());
    }
}