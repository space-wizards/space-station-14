using Content.Client.UserInterface.Controls;
using Content.Shared.Chemistry.Components;
using Content.Shared.Containers.ItemSlots;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Chemistry.UI;

/// <summary>
/// Initializes a <see cref="ReagentDispenserWindow"/> and updates it when new server messages are received.
/// </summary>
[UsedImplicitly]
public sealed class ReagentDispenserBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private ReagentDispenserWindow? _window;

    /// <summary>
    /// Called each time a dispenser UI instance is opened. Generates the dispenser window and fills it with
    /// relevant info. Sets the actions for static buttons.
    /// </summary>
    protected override void Open()
    {
        base.Open();

        // Setup window layout/elements
        _window = this.CreateWindow<ReagentDispenserWindow>();
        _window.SetInfoFromEntity(EntMan, Owner);

        // Setup static button actions.
        _window.EjectButton.OnPressed += _ =>
            SendPredictedMessage(new ItemSlotButtonPressedEvent(ReagentDispenserComponent.OutputSlotName));
        _window.ClearButton.OnPressed += _ => SendPredictedMessage(new ReagentDispenserClearContainerSolutionMessage());

        _window.AmountGrid.OnButtonPressed += OnAmountGridButtonPressed;

        _window.OnDispenseReagentButtonPressed += location =>
            SendPredictedMessage(new ReagentDispenserDispenseReagentMessage(location));
        _window.OnEjectJugButtonPressed +=
            location => SendPredictedMessage(new ReagentDispenserEjectContainerMessage(location));
    }

    private void OnAmountGridButtonPressed(string label)
    {
        if (!float.TryParse(label, out var amount))
            return;

        SendPredictedMessage(new ReagentDispenserSetDispenseAmountMessage(amount));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not ReagentDispenserBoundUserInterfaceState dispenserState)
            return;

        _window?.UpdateState(dispenserState);
    }
}
