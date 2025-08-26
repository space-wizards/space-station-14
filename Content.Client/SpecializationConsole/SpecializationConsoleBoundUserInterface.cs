
using Content.Shared.Containers.ItemSlots;
using Content.Shared.SpecializationConsole;
using Robust.Client.UserInterface;
using static Content.Shared.SpecializationConsole.SpecializationConsoleComponent;

namespace Content.Client.SpecializationConsole;

public sealed class SpecializationConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private SpecializationConsoleWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<SpecializationConsoleWindow>();
        _window.SetOwner(Owner);
        _window.OpenCentered();

        _window.PrivilegedIdButton.OnPressed += _ => SendPredictedMessage(new ItemSlotButtonPressedEvent(PrivilegedIdCardSlotId));
        _window.TargetIdButton.OnPressed += _ => SendPredictedMessage(new ItemSlotButtonPressedEvent(TargetIdCardSlotId));
        // _window.TargetIdButton.OnPressed += _ => SendPredictedMessage(new NewEmployeeDataEvent());

        // _window.OnDialogConfirmed += spec => SendMessage(new SpecializationChangedMessage(spec));

    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        var castState = (SpecializationConsoleBoundInterfaceState) state;
        _window?.UpdateState(castState);
    }
}
