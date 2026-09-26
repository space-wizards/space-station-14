using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Containers.ItemSlots;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Chemistry.UI;

/// <summary>
/// Initializes a <see cref="ChemMasterWindow"/> and updates it when new server messages are received.
/// </summary>
[UsedImplicitly]
public sealed class ChemMasterBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private ChemMasterWindow? _window;

    /// <summary>
    /// Called each time a chem master UI instance is opened. Generates the window and fills it with
    /// relevant info. Sets the actions for static buttons.
    /// </summary>
    protected override void Open()
    {
        base.Open();

        // Setup window layout/elements
        _window = this.CreateWindow<ChemMasterWindow>();
        _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;

        // Setup static button actions.
        _window.InputEjectButton.OnPressed += _ => SendPredictedMessage(
            new ItemSlotButtonPressedEvent(ChemMasterConstants.InputSlotName));
        _window.OutputEjectButton.OnPressed += _ => SendPredictedMessage(
            new ItemSlotButtonPressedEvent(ChemMasterConstants.OutputSlotName));
        _window.BufferTransferButton.OnPressed += _ => SendPredictedMessage(
            new ChemMasterSetModeMessage(ChemMasterMode.Transfer));
        _window.BufferDiscardButton.OnPressed += _ => SendPredictedMessage(
            new ChemMasterSetModeMessage(ChemMasterMode.Discard));
        _window.CreatePillButton.OnPressed += _ => SendPredictedMessage(
            new ChemMasterCreatePillsMessage(
                (uint) _window.PillDosage.Value,
                (uint) _window.PillNumber.Value,
                _window.LabelLine));
        _window.CreateBottleButton.OnPressed += _ => SendPredictedMessage(
            new ChemMasterOutputToBottleMessage(
                (uint) _window.BottleDosage.Value,
                _window.LabelLine));
        _window.BufferSortButton.OnPressed += _ => SendPredictedMessage(
            new ChemMasterSortingTypeCycleMessage());
        _window.OutputBufferDraw.OnPressed += _ => SendPredictedMessage(
            new ChemMasterOutputDrawSourceMessage(ChemMasterDrawSource.Internal));
        _window.OutputBeakerDraw.OnPressed += _ => SendPredictedMessage(
            new ChemMasterOutputDrawSourceMessage(ChemMasterDrawSource.External));

        for (uint i = 0; i < _window.PillTypeButtons.Length; i++)
        {
            var pillType = i;
            _window.PillTypeButtons[i].OnPressed += _ => SendPredictedMessage(new ChemMasterSetPillTypeMessage(pillType));
        }

        if (EntMan.TryGetComponent(Owner, out ChemMasterComponent? chemMaster))
            _window.SetupButtonPress((Owner, chemMaster));

        _window.OnReagentButtonPressed += (_, button) =>
            SendPredictedMessage(new ChemMasterReagentAmountButtonMessage(button.Id, button.Amount, button.IsBuffer));
    }

    /// <summary>
    /// Update the UI when requested.
    /// </summary>
    public override void Update()
    {
        base.Update();

        if (_window is null || !EntMan.TryGetComponent(Owner, out ChemMasterComponent? chemMaster))
            return;

        var ent = (Owner, chemMaster);
        _window.UpdateBufferData(ent);
        _window.UpdateContainerInfo(ent);
        _window.UpdateDosageFields(ent);
        _window.UpdatePanelInfo(ent);
    }

    // there's probably a better way to do this, i just dont know it lol
    public void UpdateUiLabels()
    {
        if (_window is null || !EntMan.TryGetComponent(Owner, out ChemMasterComponent? chemMaster))
            return;

        var ent = (Owner, chemMaster);
        _window.UpdateLabels(ent);
    }
}
