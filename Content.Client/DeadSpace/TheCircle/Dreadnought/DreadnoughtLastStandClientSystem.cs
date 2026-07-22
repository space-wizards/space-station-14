// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.TheCircle.Dreadnought;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.DeadSpace.TheCircle.Dreadnought;

public sealed class DreadnoughtLastStandClientSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    private DreadnoughtLastStandTimerControl? _timer;
    private EntityUid? _trackedEntity;
    private bool _dismissed;

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var player = _player.LocalEntity;
        if (player == null ||
            !TryComp<DreadnoughtLastStandActiveComponent>(player, out var active) ||
            active.Expired)
        {
            ClearTimer();
            _trackedEntity = null;
            _dismissed = false;
            return;
        }

        if (_trackedEntity != player)
        {
            ClearTimer();
            _trackedEntity = player;
            _dismissed = false;
        }

        if (_dismissed)
            return;

        EnsureTimer();
        var remaining = TimeSpan.FromTicks(Math.Max(0, (active.EndsAt - _timing.CurTime).Ticks));
        _timer!.TimerLabel.Text = Loc.GetString("dreadnought-last-stand-timer",
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

        _timer = new DreadnoughtLastStandTimerControl(DismissTimer);
        _ui.WindowRoot.AddChild(_timer);
        LayoutContainer.SetAnchorPreset(_timer, LayoutContainer.LayoutPreset.CenterTop);
        LayoutContainer.SetMarginTop(_timer, 12);
    }

    private void DismissTimer()
    {
        _dismissed = true;
        ClearTimer();
    }

    private void ClearTimer()
    {
        _timer?.Orphan();
        _timer = null;
    }
}
