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
        cfg = EntityQuery<AutoRoundEndingRuleComponent, ActiveGameRuleComponent>().Select(t => t.Item1).FirstOrDefault();
        return cfg != null;
    }

    private void OnRunLevelChanged(GameRunLevelChangedEvent ev)
    {
        if (!ControllerPresent(out _))
            return;

        switch (ev.New)
        {
            case GameRunLevel.InRound:
                _inRoundActive = true;
                _roundStartTime = _gameTiming.CurTime;
                _warned = false;
                Sawmill.Info($"[ARE] Entered InRound. Starting timer at {_roundStartTime}");
                break;
            default:
                _inRoundActive = false;
                _roundStartTime = null;
                _warned = false;
                Sawmill.Info("[ARE] Exited InRound or reset state.");
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

        if (!_inRoundActive || _roundStartTime is null)
            return;

        var now = _gameTiming.CurTime;
        var elapsed = (float)(now - _roundStartTime.Value).TotalSeconds;
        // Prefer rule config if present, else map component defaults
        float inRoundDelay;
        float warnThreshold;
        string sender;
        string warnMessage = "Стороны не продвигаются в бою. До сосредоточенного авиаудара: {remaining} секунд.";
        string endMessage = "Авиаудар нанесен. Бой окончен.";

        if (hasRule && ruleEarly != null)
        {
            inRoundDelay = ruleEarly.InRoundDelay;
            warnThreshold = ruleEarly.InRoundWarnThreshold;
            sender = ruleEarly.SenderName;
            warnMessage = ruleEarly.WarnMessage;
            endMessage = ruleEarly.EndMessage;
        }
        else
        {
            // hasComp must be true here by earlier guard
            inRoundDelay = comp!.InRoundDelay;
            warnThreshold = comp.InRoundWarnThreshold;
            sender = comp.SenderName;
        }

        var remaining = inRoundDelay - elapsed;

        if (!_warned && remaining <= warnThreshold && remaining > 0f)
        {
            Announce(FormatRemaining(warnMessage, remaining), sender);
            _warned = true;
            Sawmill.Info($"[ARE] Warned with {remaining:F1}s remaining.");
        }

        if (elapsed >= inRoundDelay)
        {
            Sawmill.Info("[ARE] InRound delay elapsed. Ending round.");
            if (!string.IsNullOrWhiteSpace(endMessage))
                Announce(endMessage, sender);
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
                    Sawmill.Info($"[ARE] Sync -> InRound at {_roundStartTime}");
                }
                break;
            default:
                _inRoundActive = false;
                _roundStartTime = null;
                _warned = false;
                // No spam log here to reduce noise.
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
