using Content.Shared.Anomaly;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Timing;

namespace Content.Client.Anomaly.Ui;

[UsedImplicitly]
public sealed partial class AnomalyGeneratorBoundUserInterface : BoundUserInterface
{
    private static readonly EntityTimerId RefreshTimer = new("refresh");

    [Dependency] private IGameTiming _timing = default!;

    private AnomalyGeneratorWindow? _window;
    private TimeSpan _cooldownEnd;

    public AnomalyGeneratorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<AnomalyGeneratorWindow>();
        _window.SetEntity(Owner);

        _window.OnGenerateButtonPressed += () =>
        {
            SendMessage(new AnomalyGeneratorGenerateButtonPressedEvent());
        };
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not AnomalyGeneratorUserInterfaceState msg)
            return;

        _window?.UpdateState(msg, _timing.CurTime);

        _cooldownEnd = msg.CooldownEndTime;
        ScheduleRefresh();
    }

    protected override void OnTimer(EntityTimerEvent timer)
    {
        if (timer.Id != RefreshTimer)
            return;

        _window?.UpdateTimer(_timing.CurTime);
        ScheduleRefresh();
    }

    private void ScheduleRefresh()
    {
        var remaining = _cooldownEnd - _timing.CurTime;
        if (remaining <= TimeSpan.Zero)
        {
            CancelTimer(RefreshTimer);
            return;
        }

        SetTimer(RefreshTimer, remaining < TimeSpan.FromSeconds(1)
            ? remaining
            : TimeSpan.FromSeconds(1));
    }
}
