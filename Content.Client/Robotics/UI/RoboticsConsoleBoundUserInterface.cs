using Content.Shared.Robotics;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Content.Shared.Robotics.Components;
using Robust.Shared.Timing;

namespace Content.Client.Robotics.UI;

public sealed partial class RoboticsConsoleBoundUserInterface : BoundUserInterface
{
    private static readonly EntityTimerId DestroyTimer = new("destroy");

    [ViewVariables]
    public RoboticsConsoleWindow _window = default!;

    public RoboticsConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<RoboticsConsoleWindow>();
        _window.SetEntity(Owner);

        _window.OnDisablePressed += address =>
        {
            SendMessage(new RoboticsConsoleDisableMessage(address));
        };
        _window.OnDestroyPressed += address =>
        {
            SendMessage(new RoboticsConsoleDestroyMessage(address));
        };
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not RoboticsConsoleState cast)
            return;

        _window.UpdateState(cast);

        if (EntMan.TryGetComponent<RoboticsConsoleComponent>(Owner, out var console) &&
            console.NextDestroy != TimeSpan.Zero)
            SetTimerAt(DestroyTimer, console.NextDestroy);
        else
            CancelTimer(DestroyTimer);

        UpdateDestroyButton();
    }

    protected override void OnTimer(EntityTimerEvent timer)
    {
        if (timer.Id == DestroyTimer)
            UpdateDestroyButton();
    }

    private void UpdateDestroyButton()
    {
        var remaining = TryGetTimer(DestroyTimer, out var timer) ? timer.Remaining : TimeSpan.Zero;
        _window.UpdateDestroyButton(remaining);
    }
}
