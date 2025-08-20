using Content.Shared._Starlight.MassDriver;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Starlight.MassDriver.UI;

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

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_menu == null)
            return;

        if (state is MassDriverUiState massDriverUiState)
            _menu.UpdateState(massDriverUiState);
    }
}