using Content.Shared.Robotics;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.Robotics.UI;

public sealed class RoboticsConsoleBoundUserInterface : BoundUserInterface
{
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
    }
}
