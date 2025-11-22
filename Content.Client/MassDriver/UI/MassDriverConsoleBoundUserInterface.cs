using Content.Shared.MassDriver;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.MassDriver.UI;

[UsedImplicitly]
public sealed class MassDriverConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private MassDriverConsoleMenu? _menu;

    public MassDriverConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<MassDriverConsoleMenu>();

        _menu.OnLaunchButtonPressed += () => SendMessage(new MassDriverLaunchMessage());

        _menu.OnModeButtonPressed += i => SendMessage(new MassDriverModeMessage(i));

        _menu.OnThrowDistance += i => SendMessage(new MassDriverThrowDistanceMessage(i));

        _menu.OnThrowSpeed += i => SendMessage(new MassDriverThrowSpeedMessage(i));
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        if (message is MassDriverUpdateUIMessage massDriverMessage)
            _menu?.UpdateState(massDriverMessage.State);
    }
}
