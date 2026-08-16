using System.Diagnostics.CodeAnalysis;
using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.DragDrop;
using Content.Shared.FixedPoint;
using Content.Shared.Labels.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Chemistry.EntitySystems;

/// <summary>
/// This handles shared logic for ChemMasters.
/// <seealso cref="ChemMasterComponent"/>
/// </summary>
public abstract partial class SharedChemMasterSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = null!;
    [Dependency] private ISharedAdminLogManager _adminLogger = null!;
    [Dependency] private ItemSlotsSystem _itemSlotsSystem = null!;
    [Dependency] private LabelSystem _labelSystem = null!;
    [Dependency] private SharedAudioSystem _audioSystem = null!;
    [Dependency] private SharedPopupSystem _popupSystem = null!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainerSystem = null!;
    [Dependency] private SharedStorageSystem _storageSystem = null!;

    protected static readonly EntProtoId PillPrototypeId = "Pill";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChemMasterComponent, ComponentStartup>(SubscribeUpdateUiState);
        SubscribeLocalEvent<ChemMasterComponent, SolutionChangedEvent>(SubscribeUpdateUiState);
        SubscribeLocalEvent<ChemMasterComponent, EntInsertedIntoContainerMessage>(SubscribeUpdateUiState);
        SubscribeLocalEvent<ChemMasterComponent, EntRemovedFromContainerMessage>(SubscribeUpdateUiState);
        // Subscribing to DragDropTargetEvent is a quick fix to ensure the UI updates when fluids are dragged and dropped into the ChemMaster, since Shared.Fluids.EntitySystems.SolutionDumpingSystem.cs bypasses UpdateChemicals().
        // TODO: Remove when proper support for infinite volume solutions is added.
        SubscribeLocalEvent<ChemMasterComponent, DragDropTargetEvent>(SubscribeUpdateUiState);
        SubscribeLocalEvent<ChemMasterComponent, BoundUIOpenedEvent>(SubscribeUpdateUiState);
    }

    protected virtual void UpdateUi(Entity<ChemMasterComponent> ent)
    {
    }

    protected virtual void UpdateUiLabels(Entity<ChemMasterComponent> ent)
    {
    }

    private void SubscribeUpdateUiState<T>(Entity<ChemMasterComponent> ent, ref T _)
    {
        UpdateUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnSetModeMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterSetModeMessage message)
    {
        // Ensure the mode is valid, either Transfer or Discard.
        if (!Enum.IsDefined(typeof(ChemMasterMode), message.ChemMasterMode))
            return;

        chemMaster.Comp.Mode = message.ChemMasterMode;
        Dirty(chemMaster);
        ClickSound(chemMaster, message.Actor);
    }

    [SubscribeLocalEvent]
    private void OnCycleSortingTypeMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterSortingTypeCycleMessage message)
    {
        chemMaster.Comp.SortingType++;
        if (chemMaster.Comp.SortingType > ChemMasterSortingType.Latest)
            chemMaster.Comp.SortingType = ChemMasterSortingType.None;
        Dirty(chemMaster);
        ClickSound(chemMaster, message.Actor);
    }

    [SubscribeLocalEvent]
    private void OnSetPillTypeMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterSetPillTypeMessage message)
    {
        // Ensure valid pill type. There are 20 pills selectable, 0-19.
        if (message.PillType > SharedChemMaster.PillTypes - 1)
            return;

        chemMaster.Comp.PillType = message.PillType;
        Dirty(chemMaster);
        ClickSound(chemMaster, message.Actor);
    }

    [SubscribeLocalEvent]
    private void OnReagentButtonMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterReagentAmountButtonMessage message)
    {
        // Ensure the amount corresponds to one of the reagent amount buttons.
        if (!Enum.IsDefined(typeof(ChemMasterReagentAmount), message.Amount))
            return;

        switch (chemMaster.Comp.Mode)
        {
            case ChemMasterMode.Transfer:
                TransferReagents(chemMaster, message.ReagentId, message.Amount.GetFixedPoint(), message.FromBuffer);
                break;
            case ChemMasterMode.Discard:
                DiscardReagents(chemMaster, message.ReagentId, message.Amount.GetFixedPoint(), message.FromBuffer);
                break;
            default:
                // Invalid mode.
                return;
        }

        ClickSound(chemMaster, message.Actor);
    }

    [SubscribeLocalEvent]
    private void OnSetDrawSourceMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterOutputDrawSourceMessage message)
    {
        //Ensure draw source is valid, either from the internal buffer or the inserted beaker
        if (!Enum.IsDefined(message.DrawSource))
            return;

        chemMaster.Comp.DrawSource = message.DrawSource;
        Dirty(chemMaster);
        ClickSound(chemMaster, message.Actor);
    }

    [SubscribeLocalEvent]
    private void OnCreatePillsMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterCreatePillsMessage message)
    {
        var user = message.Actor;
        var maybeContainer = _itemSlotsSystem.GetItemOrNull(chemMaster, SharedChemMaster.OutputSlotName);
        if (maybeContainer is not { Valid: true } container
            || !TryComp(container, out StorageComponent? storage))
        {
            return; // output can't fit pills
        }

        // Ensure the number is valid.
        if (message.Number == 0 || !_storageSystem.HasSpace((container, storage)))
            return;

        // Ensure the amount is valid.
        if (message.Dosage == 0 || message.Dosage > chemMaster.Comp.PillDosageLimit)
            return;

        // Ensure label length is within the character limit.
        if (message.Label.Length > SharedChemMaster.LabelMaxLength)
            return;

        var needed = message.Dosage * message.Number;

        if (!WithdrawFromSource(chemMaster, needed, user, out var withdrawal))
            return;
        _labelSystem.Label(container, message.Label);

        for (var i = 0; i < message.Number; i++)
        {
            var item = Spawn(PillPrototypeId, Transform(container).Coordinates);
            _storageSystem.Insert(container, item, out _, user: user, storage);
            _labelSystem.Label(item, message.Label);

            _solutionContainerSystem.EnsureSolution(item, SharedChemMaster.PillSolutionName, out var itemSolution);
            itemSolution.Comp.Solution.MaxVolume = message.Dosage;

            _solutionContainerSystem.TryAddSolution(itemSolution, withdrawal.SplitSolution(message.Dosage));

            var pill = EnsureComp<PillComponent>(item);
            pill.PillType = chemMaster.Comp.PillType;
            Dirty(item, pill);

            // Log pill creation by a user
            _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(user):user} printed {ToPrettyString(item):pill} {SharedSolutionContainerSystem.ToPrettyString(itemSolution.Comp.Solution)}");
        }

        Dirty(chemMaster);
        ClickSound(chemMaster, message.Actor);
    }

    [SubscribeLocalEvent]
    private void OnOutputToBottleMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterOutputToBottleMessage message)
    {
        var user = message.Actor;
        var maybeContainer = _itemSlotsSystem.GetItemOrNull(chemMaster, SharedChemMaster.OutputSlotName);
        if (maybeContainer is not { Valid: true } container
            || !_solutionContainerSystem.TryGetSolution(container, SharedChemMaster.BottleSolutionName, out var soln, out var solution))
        {
            return; // output can't fit reagents
        }

        // Ensure the amount is valid.
        if (message.Dosage == 0 || message.Dosage > solution.AvailableVolume)
            return;

        // Ensure label length is within the character limit.
        if (message.Label.Length > SharedChemMaster.LabelMaxLength)
            return;

        if (!WithdrawFromSource(chemMaster, message.Dosage, user, out var withdrawal))
            return;

        _labelSystem.Label(container, message.Label);
        _solutionContainerSystem.TryAddSolution(soln.Value, withdrawal);

        // Log bottle creation by a user
        _adminLogger.Add(LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(user):user} bottled {ToPrettyString(container):bottle} {SharedSolutionContainerSystem.ToPrettyString(solution)}");

        Dirty(chemMaster);
        ClickSound(chemMaster, message.Actor);
    }

    private bool WithdrawFromSource(
        Entity<ChemMasterComponent> chemMaster,
        FixedPoint2 neededVolume,
        EntityUid? user,
        [NotNullWhen(returnValue: true)] out Solution? outputSolution)
    {
        outputSolution = null;

        Solution? solution;
        Entity<SolutionComponent>? soln = null;

        switch (chemMaster.Comp.DrawSource)
        {
            case ChemMasterDrawSource.Internal:
                if (!_solutionContainerSystem.TryGetSolution(chemMaster.Owner, SharedChemMaster.BufferSolutionName, out _, out solution))
                    return false;

                if (solution.Volume == 0)
                {
                    if (user is { } uid)
                        _popupSystem.PopupCursor(Loc.GetString("chem-master-window-buffer-empty-text"), uid);

                    return false;
                }
                if (neededVolume > solution.Volume)
                {
                    if (user is { } uid)
                        _popupSystem.PopupCursor(Loc.GetString("chem-master-window-buffer-low-text"), uid);

                    return false;
                }

                break;

            case ChemMasterDrawSource.External:
                if (_itemSlotsSystem.GetItemOrNull(chemMaster, SharedChemMaster.InputSlotName) is not {} container)
                {
                    if (user.HasValue)
                        _popupSystem.PopupCursor(Loc.GetString("chem-master-window-no-beaker-text"), user.Value);
                    return false;
                }

                if (!_solutionContainerSystem.TryGetFitsInDispenser(container, out soln, out solution))
                    return false;

                if (solution.Volume == 0)
                {
                    if (user is { } uid)
                        _popupSystem.PopupCursor(Loc.GetString("chem-master-window-beaker-empty-text"), uid);

                    return false;
                }
                if (neededVolume > solution.Volume)
                {
                    if (user is { } uid)
                        _popupSystem.PopupCursor(Loc.GetString("chem-master-window-beaker-low-text"), uid);

                    return false;
                }

                break;

            default:
                return false;
        }

        outputSolution = solution.SplitSolution(neededVolume);

        if (soln.HasValue)
            _solutionContainerSystem.UpdateChemicals(soln.Value);

        return true;
    }
}
