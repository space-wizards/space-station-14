using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Server.GameTicking.Rules.Components;
using Robust.Shared.Timing;

namespace Content.Server.GameTicking.Systems;

/// <summary>
/// Drives automatic round end and restart based on presence of a map-placed
/// <see cref="AutoRoundControllerComponent"/>. Does not rely on GamePreset rules.
/// </summary>
public sealed class AutoRoundControllerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    private TimeSpan? _roundStartTime;
    private bool _inRoundActive;
    private bool _inRoundWarned;

    private TimeSpan? _roundEndTime;
    private bool _postRoundActive;
    private bool _postRoundWarned;

    private static readonly ISawmill Sawmill = Logger.GetSawmill("auto-round-controller");

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChanged);
    }

    private bool ControllerPresent()
    {
        return EntityQuery<AutoRoundControllerComponent>().Any();
    }

    private void OnRunLevelChanged(GameRunLevelChangedEvent ev)
    {
        if (!ControllerPresent())
            return;

        switch (ev.New)
        {
            case GameRunLevel.InRound:
                _inRoundActive = true;
                _roundStartTime = _gameTiming.CurTime;
                _inRoundWarned = false;
                break;
            case GameRunLevel.PostRound:
                _postRoundActive = true;
                _roundEndTime = _gameTiming.CurTime;
                _postRoundWarned = false;
                // Leaving InRound -> ensure we don't double-handle
                _inRoundActive = false;
                _roundStartTime = null;
                _inRoundWarned = false;
                break;
            default:
                // Any other state: reset
                _inRoundActive = false;
                _roundStartTime = null;
                _inRoundWarned = false;
                _postRoundActive = false;
                _roundEndTime = null;
                _postRoundWarned = false;
                break;
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!ControllerPresent())
            return;

        // If the new GameRules are active, make this legacy controller inert to avoid duplicate messages/actions.
        var hasEndingRule = EntityQuery<AutoRoundEndingRuleComponent, ActiveGameRuleComponent>().Any();
        var hasRestartRule = EntityQuery<AutoRoundRestartRuleComponent, ActiveGameRuleComponent>().Any();
        if (hasEndingRule || hasRestartRule)
            return;

        // Ensure our internal state matches the current RunLevel even if the controller
        // spawned after the last RunLevel change (e.g., from station initialization).
        SyncStateWithCurrentRunLevel();

        // Read settings from the first controller (if multiple exist, the first defines timings)
        var controller = EntityQuery<AutoRoundControllerComponent>().FirstOrDefault();
        if (controller == null)
            return;

        var now = _gameTiming.CurTime;

        // InRound handling -> trigger EndRound
        if (controller.EnableInRoundAutoEnd && _inRoundActive && _roundStartTime is TimeSpan startTs)
        {
            var elapsed = (float)(now - startTs).TotalSeconds;
            var remaining = controller.InRoundDelay - elapsed;

            if (!_inRoundWarned && remaining <= controller.InRoundWarnThreshold && remaining > 0f)
            {
                if (!string.IsNullOrWhiteSpace(controller.InRoundWarnMessage))
                    Announce(FormatRemaining(controller.InRoundWarnMessage, remaining), controller.SenderName);
                _inRoundWarned = true;
            }

            if (elapsed >= controller.InRoundDelay)
            {
                Sawmill.Info("[AutoRoundController] InRound delay elapsed, ending round.");
                _gameTicker.EndRound();
                _inRoundActive = false;
                _roundStartTime = null;
                _inRoundWarned = false;
            }
        }

        // PostRound handling -> trigger RestartRound
        if (controller.EnablePostRoundAutoRestart && _postRoundActive && _roundEndTime is TimeSpan endTs)
        {
            var elapsed = (float)(now - endTs).TotalSeconds;
            var remaining = controller.PostRoundDelay - elapsed;

            if (!_postRoundWarned && remaining <= controller.PostRoundWarnThreshold && remaining > 0f)
            {
                if (!string.IsNullOrWhiteSpace(controller.PostRoundWarnMessage))
                    Announce(FormatRemaining(controller.PostRoundWarnMessage, remaining), controller.SenderName);
                _postRoundWarned = true;
            }

            if (elapsed >= controller.PostRoundDelay)
            {
                Sawmill.Info("[AutoRoundController] PostRound delay elapsed, restarting round.");
                _gameTicker.RestartRound();
                _postRoundActive = false;
                _roundEndTime = null;
                _postRoundWarned = false;
            }
        }
    }

    private void Announce(string msg, string sender)
    {
        _chat.DispatchGlobalAnnouncement(msg, sender: sender);
        Sawmill.Info($"[AutoRoundController] {msg}");
    }

    private static string FormatRemaining(string template, float remaining)
    {
        var rem = MathF.Ceiling(MathF.Max(0f, remaining));
        return template.Replace("{remaining}", rem.ToString());
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
                    _inRoundWarned = false;
                    // Make sure post-round timers are off
                    _postRoundActive = false;
                    _roundEndTime = null;
                    _postRoundWarned = false;
                }
                break;
            case GameRunLevel.PostRound:
                if (!_postRoundActive)
                {
                    _postRoundActive = true;
                    _roundEndTime = _gameTiming.CurTime;
                    _postRoundWarned = false;
                    // Make sure in-round timers are off
                    _inRoundActive = false;
                    _roundStartTime = null;
                    _inRoundWarned = false;
                }
                break;
            default:
                // Reset when not in InRound / PostRound
                _inRoundActive = false;
                _roundStartTime = null;
                _inRoundWarned = false;
                _postRoundActive = false;
                _roundEndTime = null;
                _postRoundWarned = false;
                break;
        }
    }
}
