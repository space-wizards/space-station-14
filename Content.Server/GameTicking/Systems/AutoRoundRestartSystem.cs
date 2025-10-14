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
    private readonly HashSet<float> _warnedThresholds = new();

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
        // Prefer an active rule whose prototype ID is present in the current map's MapAutoGameRule rules.
        cfg = null;
        var active = EntityQuery<AutoRoundRestartRuleComponent, ActiveGameRuleComponent>().ToList();
        if (active.Count == 0)
            return false;

        var mapCfg = EntityQuery<MapAutoGameRuleComponent>().FirstOrDefault();
        if (mapCfg != null && mapCfg.Rules.Count > 0)
        {
            foreach (var (ruleComp, activeComp) in active)
            {
                if (!TryComp<MetaDataComponent>(activeComp.Owner, out var meta))
                    continue;
                var id = meta.EntityPrototype?.ID;
                if (id != null && mapCfg.Rules.Contains(id))
                {
                    cfg = ruleComp;
                    break;
                }
            }
        }

        // Fallback to first active rule if none matched.
        cfg ??= active.FirstOrDefault().Item1;
        return cfg != null;
    }

    private void OnRunLevelChanged(GameRunLevelChangedEvent ev)
    {
        // Only react if an AutoRoundRestartRule prototype is active.
        if (!TryGetRuleConfig(out _))
            return;

        switch (ev.New)
        {
            case GameRunLevel.PostRound:
                _postRoundActive = true;
                _roundEndTime = _gameTiming.CurTime;
                _warned = false;
                _warnedThresholds.Clear();
                break;
            default:
                _postRoundActive = false;
                _roundEndTime = null;
                _warned = false;
                _warnedThresholds.Clear();
                break;
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var hasRule = TryGetRuleConfig(out var ruleEarly);
        if (!hasRule)
            return;

        // Sync state in case component spawns late
        SyncStateWithCurrentRunLevel();

        if (!_postRoundActive || _roundEndTime is null)
            return;

        var now = _gameTiming.CurTime;
        var elapsed = (float)(now - _roundEndTime.Value).TotalSeconds;
        // Use only the active rule prototype. Without it, no announcements are allowed.
        float postDelay;
        float warnThreshold;
        string sender;
        string? warnMessage = null;    // Only from prototype
        string? restartMessage = null; // Only from prototype
        List<float>? thresholdsList = null;
        List<string>? thresholdMessages = null;

        postDelay = ruleEarly!.PostRoundDelay;
        warnThreshold = ruleEarly.PostRoundWarnThreshold;
        sender = ruleEarly.SenderName;
        warnMessage = string.IsNullOrWhiteSpace(ruleEarly.WarnMessage) ? null : ruleEarly.WarnMessage;
        restartMessage = string.IsNullOrWhiteSpace(ruleEarly.RestartMessage) ? null : ruleEarly.RestartMessage;
        thresholdsList = ruleEarly.WarnThresholds is { Count: > 0 } ? ruleEarly.WarnThresholds : null;
        thresholdMessages = ruleEarly.WarnMessages;

        var remaining = postDelay - elapsed;

        // Multi-threshold support like ending system
        if (thresholdsList != null && thresholdsList.Count > 0)
        {
            foreach (var th in thresholdsList.OrderByDescending(t => t))
            {
                if (remaining <= th && remaining > 0f && !_warnedThresholds.Contains(th))
                {
                    string msg;
                    if (thresholdMessages != null && thresholdMessages.Count == thresholdsList.Count)
                    {
                        var idx = thresholdsList.IndexOf(th);
                        msg = thresholdMessages[idx];
                    }
                    else
                    {
                        msg = warnMessage ?? string.Empty;
                    }
                    if (!string.IsNullOrWhiteSpace(msg))
                        Announce(FormatRemaining(msg, remaining), sender);
                    _warnedThresholds.Add(th);
                }
            }
        }
        else
        {
            if (!_warned && remaining <= warnThreshold && remaining > 0f && !string.IsNullOrWhiteSpace(warnMessage))
            {
                Announce(FormatRemaining(warnMessage!, remaining), sender);
                _warned = true;
            }
        }

        if (elapsed >= postDelay)
        {
            if (!string.IsNullOrWhiteSpace(restartMessage))
                Announce(restartMessage!, sender);
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
