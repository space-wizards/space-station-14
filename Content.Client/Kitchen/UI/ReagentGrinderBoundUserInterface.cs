using Content.Shared.Containers.ItemSlots;
using Content.Shared.Kitchen;
using Content.Shared.Kitchen.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Kitchen.UI;

[UsedImplicitly]
public sealed class ReagentGrinderBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables] private GrinderMenu? _menu;

    protected override void Open()
    {
        base.Open();
        _menu = this.CreateWindow<GrinderMenu>();
        _menu.SetEntity(Owner);

        _menu.OnToggleAuto += () => SendPredictedMessage(new ReagentGrinderToggleAutoModeMessage());
        _menu.OnGrind += () => SendPredictedMessage(new ReagentGrinderStartMessage(GrinderProgram.Grind));
        _menu.OnJuice += () => SendPredictedMessage(new ReagentGrinderStartMessage(GrinderProgram.Juice));
        _menu.OnEjectAll += () => SendPredictedMessage(new ReagentGrinderEjectChamberAllMessage());
        _menu.OnEjectBeaker += () => SendPredictedMessage(new ItemSlotButtonPressedEvent(ReagentGrinderComponent.BeakerSlotId));
        _menu.OnEjectChamber += uid => SendPredictedMessage(new ReagentGrinderEjectChamberContentMessage(EntMan.GetNetEntity(uid)));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not ReagentGrinderUpdateUserInterfaceState grinderState)
            return;

        _menu?.UpdateUi(grinderState);
    }
}
