using Content.Replay.Manager;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Timing;

namespace Content.Replay.UI.TimeWidget;

public sealed class TimeController : UIController
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ReplayManager _replay = default!;

    private TimeControlBox? TimeBox => UIManager.GetActiveUIWidgetOrNull<TimeControlBox>();

    public override void Initialize()
    {
    }

    public override void FrameUpdate(FrameEventArgs args)
    {
        var tardis = TimeBox;
        if (tardis == null)
            return;

        if (_replay.CurrentReplay is not { } replay)
        {
            tardis.Visible = false;
            return;
        }

        if (!tardis.TickSlider.Grabbed)
            tardis.TickSlider.SetValueWithoutEvent(replay.CurrentIndex);

        var index = (int) tardis.TickSlider.Value;
        _replay.ScrubbingIndex = index;

        tardis.Visible = true;
        tardis.TickSlider.MinValue = 0;
        tardis.TickSlider.MaxValue = replay.States.Count - 1;

        var state = replay.States[index];
        var divisor = Math.Max(1, replay.States.Count - 1);
        var percentage = (100 * (float) index / divisor).ToString("F2");

        tardis.IndexLabel.Text = $"Index: {index} / {divisor} ({percentage}%)";
        tardis.TickLabel.Text = $"Tick: {state.ToSequence} / {replay.States[^1].ToSequence}";

        if (index == replay.CurrentIndex || tardis.DynamicScrubbingCheckbox.Pressed)
        {
            var serverTime = _timing.CurTime;
            var playTime = serverTime - replay.StartTime;
            tardis.TimeLabel.Text = $"Recording Time: {playTime:hh\\:mm\\:ss} / {replay.Duration:hh\\:mm\\:ss}";
            tardis.ServerTimeLabel.Text = $"Server Time: {serverTime:hh\\:mm\\:ss}";
        }
        else
        {
            tardis.TimeLabel.Text = $"Play Time: NA / {replay.Duration:hh\\:mm\\:ss}";
            tardis.ServerTimeLabel.Text = $"Server Time: NA";
        }

        tardis.PlayButton.Pressed = _replay.Playing;
        tardis.ResetButton.Disabled = replay.RewindDisabled;
        tardis.RewindButton.Disabled = replay.RewindDisabled;
        tardis.RewindFiveButton.Disabled = replay.RewindDisabled;
        tardis.CheckpointLabel.Text = $"Nearest checkpoint: {_replay.GetLastCheckpoint(replay, index).Index}";
        _replay.ActivelyScrubbing = tardis.DynamicScrubbingCheckbox.Pressed && tardis.TickSlider.Grabbed;
    }
}
