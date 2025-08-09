using Content.Client.Chemistry.Containers.EntitySystems;
using Content.Client.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Containers.ItemSlots;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Chemistry.UI.ChemMaster;

/// <summary>
/// Initializes a <see cref="ChemMasterWindow"/> and updates it when new server messages are received.
/// </summary>
[UsedImplicitly]
public sealed class ChemMasterBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private ChemMasterWindow? _window;

    private readonly ChemMasterSystem _chem;
    private readonly SolutionContainerSystem _solContainer;

    public ChemMasterBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _chem = EntMan.System<ChemMasterSystem>();
        _solContainer = EntMan.System<SolutionContainerSystem>();
    }

    /// <summary>
    /// Called each time a chem master UI instance is opened. Generates the window and fills it with
    /// relevant info. Sets up necessary action subscriptions.
    /// </summary>
    protected override void Open()
    {
        base.Open();

        // Setup window layout/elements
        _window = this.CreateWindow<ChemMasterWindow>();
        _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;

        // Setup static button actions.
        _window.OnReagentButton += args =>
            SendPredictedMessage(new ChemMasterReagentAmountButtonMessage(args.Id, args.Amount, args.IsBuffer));
        _window.OnModeButton += mode => SendPredictedMessage(new ChemMasterSetModeMessage(mode));
        _window.OnSortButton += () => SendPredictedMessage(new ChemMasterSortingTypeCycleMessage());
        _window.OnEjectButton += slot => SendPredictedMessage(new ItemSlotButtonPressedEvent(slot));

        _window.OnPillButton += index => SendPredictedMessage(new ChemMasterSetPillTypeMessage(index));
        _window.OnCreatePill += args =>
            SendPredictedMessage(new ChemMasterCreatePillsMessage(args.Dosage, args.Count, args.Label));
        _window.OnCreateBottle += args =>
            SendPredictedMessage(new ChemMasterOutputToBottleMessage(args.Dosage, args.Label));

        Update();
    }

    public override void Update()
    {
        if (_window == null || !EntMan.TryGetComponent<ChemMasterComponent>(Owner, out var cm))
            return;

        if (!_solContainer.TryGetSolution(Owner,
                ChemMasterComponent.BufferSolutionName,
                out _,
                out var bufferSolution))
            return;

        var inputInfo = _chem.BuildInputContainerInfo((Owner, cm));
        var outputInfo = _chem.BuildOutputContainerInfo((Owner, cm));

        _window.UpdateBuffer(bufferSolution, cm.Mode, cm.SortingType);
        _window.SetInputContainerInfo(inputInfo);
        _window.SetOutputContainerInfo(outputInfo);
        _window.UpdateDosageFields(bufferSolution,
            outputInfo,
            cm.OutputLabel,
            cm.PillType,
            cm.PillDosageLimit);
    }
}
