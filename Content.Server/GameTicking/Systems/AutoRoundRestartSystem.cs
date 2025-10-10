using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules.Components;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Timing;

namespace Content.Server.GameTicking.Systems;

/// <summary>
/// Restarts the round automatically after a configured delay when an
/// <see cref="AutoRoundRestartComponent"/> exists on the map. Place on Map Entity in Resources/Maps.
/// </summary>
public sealed class AutoRoundRestartSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    private TimeSpan? _roundEndTime;
    private bool _postRoundActive;
    private bool _warned;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChanged);
    }

    private bool ControllerPresent(out AutoRoundRestartComponent? comp)
    {
        comp = EntityQuery<AutoRoundRestartComponent>().FirstOrDefault();
        return comp != null && comp.Enabled;
    }

    private bool TryGetRuleConfig([NotNullWhen(true)] out AutoRoundRestartRuleComponent? cfg)
    {
        cfg = EntityQuery<AutoRoundRestartRuleComponent, ActiveGameRuleComponent>().Select(t => t.Item1).FirstOrDefault();
        return cfg != null;
    }

    private void OnRunLevelChanged(GameRunLevelChangedEvent ev)
    {
        if (!ControllerPresent(out _))
            return;

        switch (ev.New)
        {
            case GameRunLevel.PostRound:
                _postRoundActive = true;
                _roundEndTime = _gameTiming.CurTime;
                _warned = false;
                break;
            default:
                _postRoundActive = false;
                _roundEndTime = null;
                _warned = false;
                break;
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var hasRule = TryGetRuleConfig(out var ruleEarly);
        var hasComp = ControllerPresent(out var comp);
        if (!hasRule && !hasComp)
            return;

        // Sync state in case component spawns late
        SyncStateWithCurrentRunLevel();

        if (!_postRoundActive || _roundEndTime is null)
            return;

        var now = _gameTiming.CurTime;
        var elapsed = (float)(now - _roundEndTime.Value).TotalSeconds;
        // Prefer rule config if present
        float postDelay;
        float warnThreshold;
        string sender;
        string warnMessage = "Авиаудар нанесен. Конец боя через: {remaining} секунд!";
        string restartMessage = "Новый раунд начинается!";

        if (hasRule && ruleEarly != null)
        {
            postDelay = ruleEarly.PostRoundDelay;
            warnThreshold = ruleEarly.PostRoundWarnThreshold;
            sender = ruleEarly.SenderName;
            warnMessage = ruleEarly.WarnMessage;
            restartMessage = ruleEarly.RestartMessage;
        }
        else
        {
            // hasComp must be true here by earlier guard
            postDelay = comp!.PostRoundDelay;
            warnThreshold = comp.PostRoundWarnThreshold;
            sender = comp.SenderName;
        }

        var remaining = postDelay - elapsed;

        if (!_warned && remaining <= warnThreshold && remaining > 0f)
        {
            Announce(FormatRemaining(warnMessage, remaining), sender);
            _warned = true;
        }

        if (elapsed >= postDelay)
        {
            if (!string.IsNullOrWhiteSpace(restartMessage))
                Announce(restartMessage, sender);
            _gameTicker.RestartRound();
            _postRoundActive = false;
            _roundEndTime = null;
            _warned = false;
        }
    }

    private void SyncStateWithCurrentRunLevel()
    {
        var level = _gameTicker.RunLevel;
        switch (level)
        {
            case GameRunLevel.PostRound:
                if (!_postRoundActive)
                {
                    _postRoundActive = true;
                    _roundEndTime = _gameTiming.CurTime;
                    _warned = false;
                }
                break;
            default:
                _postRoundActive = false;
                _roundEndTime = null;
                _warned = false;
                break;
        }
    }

    private void Announce(string msg, string sender)
    {
        _chat.DispatchGlobalAnnouncement(msg, sender: sender);
    }

    private static string FormatRemaining(string template, float remaining)
    {
        var rem = MathF.Ceiling(MathF.Max(0f, remaining));
        return template.Replace("{remaining}", rem.ToString());
    }
}
