using Content.Shared.Anomaly;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Timing;

namespace Content.Client.Anomaly.Ui;

[UsedImplicitly]
public sealed partial class AnomalyGeneratorBoundUserInterface : BoundUserInterface
{
    private static readonly EntityTimerId CooldownTimer = new("cooldown");
    private static readonly EntityTimerId RefreshTimer = new("refresh");

    private AnomalyGeneratorWindow? _window;

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

        SetTimerAt(CooldownTimer, msg.CooldownEndTime);
        SetTimer(RefreshTimer, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        _window?.UpdateState(msg, GetRemaining());
    }

    protected override void OnTimer(EntityTimerEvent timer)
    {
        if (timer.Id == CooldownTimer)
            CancelTimer(RefreshTimer);

        if (timer.Id == CooldownTimer || timer.Id == RefreshTimer)
            _window?.UpdateTimer(GetRemaining());
    }

    private TimeSpan GetRemaining()
    {
        return TryGetTimer(CooldownTimer, out var timer) ? timer.Remaining : TimeSpan.Zero;
    }
}
