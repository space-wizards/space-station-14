// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.TheCircle.Shuttles;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.DeadSpace.TheCircle.Shuttles;

public sealed class CircleShuttleTimerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private CircleShuttleTimerControl? _timer;

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (_player.LocalEntity is not { } player || _transform.GetGrid(player) is not { } grid)
        {
            ClearTimer();
            return;
        }

        TimeSpan? unlockAt = null;
        if (TryComp<CirclePrimaryShuttleComponent>(grid, out var primary) &&
            primary.TimerStarted &&
            !primary.Unlocked)
        {
            unlockAt = primary.UnlockAt;
        }
        else if (TryComp<CircleSecondaryShuttleComponent>(grid, out var secondary) &&
                 secondary.TimerStarted &&
                 !secondary.Unlocked)
        {
            unlockAt = secondary.UnlockAt;
        }

        if (unlockAt == null)
        {
            ClearTimer();
            return;
        }

        EnsureTimer();
        var remaining = TimeSpan.FromTicks(Math.Max(0, (unlockAt.Value - _timing.CurTime).Ticks));
        _timer!.TimerLabel.Text = Loc.GetString("circle-shuttle-ftl-unlock-timer",
            ("time", remaining.ToString(@"mm\:ss")));
    }

    public override void Shutdown()
    {
        ClearTimer();
        base.Shutdown();
    }

    private void EnsureTimer()
    {
        if (_timer?.Parent != null)
            return;

        _timer = new CircleShuttleTimerControl();
        _ui.WindowRoot.AddChild(_timer);
        LayoutContainer.SetAnchorPreset(_timer, LayoutContainer.LayoutPreset.CenterTop);
        LayoutContainer.SetMarginTop(_timer, 52);
    }

    private void ClearTimer()
    {
        _timer?.Orphan();
        _timer = null;
    }
}
