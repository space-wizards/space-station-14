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
/// Ends the round automatically after a configured delay when an
/// <see cref="AutoRoundEndingComponent"/> exists on the map. Place on Map Entity in Resources/Maps.
/// </summary>
public sealed class AutoRoundEndingSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    private static readonly ISawmill Sawmill = Logger.GetSawmill("auto-round-ending");

    private TimeSpan? _roundStartTime;
    private bool _inRoundActive;
    private bool _warned;
    private readonly HashSet<float> _warnedThresholds = new();
    private bool _startAnnounced;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChanged);
    }

    private bool ControllerPresent(out AutoRoundEndingComponent? comp)
    {
        comp = EntityQuery<AutoRoundEndingComponent>().FirstOrDefault();
        return comp != null && comp.Enabled;
    }

    private bool TryGetRuleConfig([NotNullWhen(true)] out AutoRoundEndingRuleComponent? cfg)
    {
        // Prefer the active rule with the largest delay to avoid a shorter config overriding a longer one.
        cfg = EntityQuery<AutoRoundEndingRuleComponent, ActiveGameRuleComponent>()
            .Select(t => t.Item1)
            .OrderByDescending(c => c.InRoundDelay)
            .FirstOrDefault();
        return cfg != null;
    }

    private void OnRunLevelChanged(GameRunLevelChangedEvent ev)
    {
        // Activate if either a map controller is present OR an active rule exists
        var hasController = ControllerPresent(out _);
        var hasRule = EntityQuery<AutoRoundEndingRuleComponent, ActiveGameRuleComponent>().Any();
        if (!hasController && !hasRule)
            return;

        switch (ev.New)
        {
            case GameRunLevel.InRound:
                _inRoundActive = true;
                _roundStartTime = _gameTiming.CurTime;
                _warned = false;
                _warnedThresholds.Clear();
                _startAnnounced = false;
                Sawmill.Info($"[ARE] Entered InRound. Starting timer at {_roundStartTime}");
                // Optional start announcement configured via AutoRoundEndingRule
                if (TryGetRuleConfig(out var startCfg) && !string.IsNullOrWhiteSpace(startCfg.StartMessage))
                {
                    // At round start, remaining time equals the full delay
                    var startMsg = FormatRemaining(startCfg.StartMessage, startCfg.InRoundDelay);
                    Announce(startMsg, startCfg.SenderName);
                    Sawmill.Info("[ARE] Start message announced from rule config.");
                    _startAnnounced = true;
                }
                // Broadcast HUD info to clients
                if (TryGetRuleConfig(out var hudCfg))
                {
                    var hudEv = new AutoRoundEndingHudEvent(
                        _roundStartTime!.Value,
                        hudCfg.InRoundDelay,
                        string.IsNullOrWhiteSpace(hudCfg.HudLabel) ? null : hudCfg.HudLabel,
                        hudCfg.HudIconRsi,
                        hudCfg.HudIconState);
                    RaiseNetworkEvent(hudEv);
                }
                break;
            default:
                _inRoundActive = false;
                _roundStartTime = null;
                _warned = false;
                _warnedThresholds.Clear();
                _startAnnounced = false;
                Sawmill.Info("[ARE] Exited InRound or reset state.");
                // Clear HUD on clients
                RaiseNetworkEvent(new AutoRoundEndingHudClearEvent());
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

        if (!_inRoundActive || _roundStartTime is null)
            return;

        var now = _gameTiming.CurTime;
        var elapsed = (float)(now - _roundStartTime.Value).TotalSeconds;
        // Use only the active rule prototype. Without it, no announcements are allowed.
        float inRoundDelay;
        float warnThreshold;
        string sender;
        string? warnMessage = null; // Only from prototype
        string? endMessage = null;  // Only from prototype
        List<float>? thresholdsList = null;
        List<string>? thresholdMessages = null;

        inRoundDelay = ruleEarly!.InRoundDelay;
        warnThreshold = ruleEarly.InRoundWarnThreshold;
        sender = ruleEarly.SenderName;
        warnMessage = string.IsNullOrWhiteSpace(ruleEarly.WarnMessage) ? null : ruleEarly.WarnMessage;
        endMessage = string.IsNullOrWhiteSpace(ruleEarly.EndMessage) ? null : ruleEarly.EndMessage;
        thresholdsList = ruleEarly.WarnThresholds is { Count: > 0 } ? ruleEarly.WarnThresholds : null;
        thresholdMessages = ruleEarly.WarnMessages;

        // Late start-message path: if rules were started after the event, send it once here.
        if (!_startAnnounced && !string.IsNullOrWhiteSpace(ruleEarly.StartMessage))
        {
            var startMsg = FormatRemaining(ruleEarly.StartMessage, ruleEarly.InRoundDelay);
            Announce(startMsg, ruleEarly.SenderName);
            _startAnnounced = true;
            Sawmill.Info("[ARE] Start message announced late from rule config.");
        }

        var remaining = inRoundDelay - elapsed;

        // Multi-threshold support (prototype-configured). If provided, supersedes single-threshold logic.
        if (thresholdsList != null && thresholdsList.Count > 0)
        {
            // sort descending so higher thresholds trigger earlier
            foreach (var th in thresholdsList.OrderByDescending(t => t))
            {
                if (remaining <= th && remaining > 0f && !_warnedThresholds.Contains(th))
                {
                    string msg;
                    if (thresholdMessages != null && thresholdMessages.Count == thresholdsList.Count)
                    {
                        // map message by index
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
                    Sawmill.Info($"[ARE] Warned at threshold {th}s with {remaining:F1}s remaining.");
                }
            }
        }
        else
        {
            if (!_warned && remaining <= warnThreshold && remaining > 0f && !string.IsNullOrWhiteSpace(warnMessage))
            {
                Announce(FormatRemaining(warnMessage, remaining), sender);
                _warned = true;
                Sawmill.Info($"[ARE] Warned with {remaining:F1}s remaining.");
            }
        }

        if (elapsed >= inRoundDelay)
        {
            Sawmill.Info("[ARE] InRound delay elapsed. Ending round.");
            if (!string.IsNullOrWhiteSpace(endMessage))
                Announce(endMessage!, sender);
            // Clear HUD before ending
            RaiseNetworkEvent(new AutoRoundEndingHudClearEvent());
            _gameTicker.EndRound();
            _inRoundActive = false;
            _roundStartTime = null;
            _warned = false;
        }
    }

    private void SyncStateWithCurrentRunLevel()
    {
        var level = _gameTicker.RunLevel;
        switch (level)
        {
            case GameRunLevel.InRound:
                if (!_inRoundActive)
                {
                    _inRoundActive = true;
                    _roundStartTime = _gameTiming.CurTime;
                    _warned = false;
                    _startAnnounced = false;
                    Sawmill.Info($"[ARE] Sync -> InRound at {_roundStartTime}");
                    // Also push HUD on sync in case clients joined late
                    if (TryGetRuleConfig(out var hudCfg))
                    {
                        var hudEv = new AutoRoundEndingHudEvent(
                            _roundStartTime!.Value,
                            hudCfg.InRoundDelay,
                            string.IsNullOrWhiteSpace(hudCfg.HudLabel) ? null : hudCfg.HudLabel,
                            hudCfg.HudIconRsi,
                            hudCfg.HudIconState);
                        RaiseNetworkEvent(hudEv);
                    }
                }
                break;
            default:
                _inRoundActive = false;
                _roundStartTime = null;
                _warned = false;
                _startAnnounced = false;
                // No spam log here to reduce noise.
                RaiseNetworkEvent(new AutoRoundEndingHudClearEvent());
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
