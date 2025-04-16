using Content.Shared.SetSelector;
using Robust.Client.UserInterface;

namespace Content.Client.SetSelector;

public sealed class SetSelectorBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private SetSelectorMenu? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<SetSelectorMenu>();
        _window.OnApprove += SendApprove;
        _window.OnSetChange += SendChangeSelected;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not SetSelectorBoundUserInterfaceState current)
            return;

        _window?.UpdateState(current);
    }

    private void SendChangeSelected(int setNumber)
    {
        SendMessage(new SetSelectorChangeSetMessage(setNumber));
    }

    private void SendApprove()
    {
        SendMessage(new SetSelectorApproveMessage());
    }
}
