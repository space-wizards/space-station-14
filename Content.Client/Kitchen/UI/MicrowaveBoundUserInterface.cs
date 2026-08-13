using Content.Shared.Kitchen.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Kitchen.UI;

[UsedImplicitly]
public sealed class MicrowaveBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables] private MicrowaveMenu? _menu;

    protected override void Open()
    {
        base.Open();
        _menu = this.CreateWindow<MicrowaveMenu>();

        _menu.StartButton.OnPressed += _ => SendPredictedMessage(new MicrowaveStartCookMessage());
        _menu.EjectButton.OnPressed += _ => SendPredictedMessage(new MicrowaveEjectMessage());
        _menu.OnEjectSolid += netEntity => SendPredictedMessage(new MicrowaveEjectSolidIndexedMessage(netEntity));

        _menu.OnCookTimeSelected += (buttonIndex, cookTime) =>
            SendPredictedMessage(new MicrowaveSelectCookTimeMessage(buttonIndex, cookTime));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not MicrowaveUpdateUserInterfaceState cState || _menu == null)
            return;

        _menu.UpdateUi(cState);
    }
}
