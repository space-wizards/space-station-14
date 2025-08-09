using System.Diagnostics.CodeAnalysis;
using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Labels.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using JetBrains.Annotations;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chemistry.EntitySystems;

/// <summary>
/// Contains all the shared logic for ChemMasters.
/// <seealso cref="ChemMasterComponent"/>
/// </summary>
[UsedImplicitly]
public abstract class SharedChemMasterSystem : EntitySystem
{
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] protected readonly SharedSolutionContainerSystem SolContainer = default!;
    [Dependency] protected readonly ItemSlotsSystem ItemSlots = default!;
    [Dependency] protected readonly SharedStorageSystem Storage = default!;
    [Dependency] private readonly LabelSystem _labelSystem = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

    private static readonly EntProtoId PillPrototypeId = "Pill";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChemMasterComponent, ComponentStartup>(SubscribeUpdateUiState);
        SubscribeLocalEvent<ChemMasterComponent, SolutionContainerChangedEvent>(SubscribeUpdateUiState);
        SubscribeLocalEvent<ChemMasterComponent, EntInsertedIntoContainerMessage>(SubscribeUpdateUiState);
        SubscribeLocalEvent<ChemMasterComponent, EntRemovedFromContainerMessage>(SubscribeUpdateUiState);
        SubscribeLocalEvent<ChemMasterComponent, BoundUIOpenedEvent>(SubscribeUpdateUiState);

        Subs.BuiEvents<ChemMasterComponent>(ChemMasterUiKey.Key,
            subs =>
            {
                subs.Event<ChemMasterSetModeMessage>(OnSetModeMessage);
                subs.Event<ChemMasterSortingTypeCycleMessage>(OnCycleSortingTypeMessage);
                subs.Event<ChemMasterSetPillTypeMessage>(OnSetPillTypeMessage);
                subs.Event<ChemMasterReagentAmountButtonMessage>(OnReagentButtonMessage);
                subs.Event<ChemMasterCreatePillsMessage>(OnCreatePillsMessage);
                subs.Event<ChemMasterOutputToBottleMessage>(OnOutputToBottleMessage);
            });
    }

    private void SubscribeUpdateUiState<T>(Entity<ChemMasterComponent> ent, ref T ev)
    {
        DirtyUI(ent);
    }

    protected virtual void DirtyUI(Entity<ChemMasterComponent> ent) { }

    private void OnSetModeMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterSetModeMessage message)
    {
        // Ensure the mode is valid, either Transfer or Discard.
        if (!Enum.IsDefined(message.ChemMasterMode))
            return;

        chemMaster.Comp.Mode = message.ChemMasterMode;
        Dirty(chemMaster);
        DirtyUI(chemMaster);
        ClickSound(chemMaster, message.Actor);
    }

    private void OnCycleSortingTypeMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterSortingTypeCycleMessage message)
    {
        chemMaster.Comp.SortingType++;
        if (chemMaster.Comp.SortingType > ChemMasterSortingType.Latest)
            chemMaster.Comp.SortingType = ChemMasterSortingType.None;

        Dirty(chemMaster);
        DirtyUI(chemMaster);
        ClickSound(chemMaster, message.Actor);
    }

    private void OnSetPillTypeMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterSetPillTypeMessage message)
    {
        // Ensure valid pill type. There are 20 pills selectable, 0-19.
        if (message.PillType > ChemMasterComponent.PillTypes - 1)
            return;

        chemMaster.Comp.PillType = message.PillType;
        Dirty(chemMaster);
        DirtyUI(chemMaster);
        ClickSound(chemMaster, message.Actor);
    }

    private void OnReagentButtonMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterReagentAmountButtonMessage message)
    {
        // Ensure the amount corresponds to one of the reagent amount buttons.
        if (!ChemMasterComponent.ChemMasterAmountOptions.Contains(message.Amount))
            return;

        switch (chemMaster.Comp.Mode)
        {
            case ChemMasterMode.Transfer:
                TransferReagents(chemMaster, message.ReagentId, message.Amount, message.FromBuffer);
                break;
            case ChemMasterMode.Discard:
                DiscardReagents(chemMaster, message.ReagentId, message.Amount, message.FromBuffer);
                break;
            default:
                // Invalid mode.
                return;
        }

        ClickSound(chemMaster, message.Actor);
    }

    private void TransferReagents(Entity<ChemMasterComponent> chemMaster, ReagentId id, FixedPoint2 amount, bool fromBuffer)
    {
        var container = ItemSlots.GetItemOrNull(chemMaster, ChemMasterComponent.InputSlotName);
        // TODO this should be resolving the solution instead of using TryGet. (Likely applies elsewhere in here.)
        if (container is null
            || !SolContainer.TryGetFitsInDispenser(container.Value,
                out var containerSoln,
                out var containerSolution)
            || !SolContainer.TryGetSolution(chemMaster.Owner,
                ChemMasterComponent.BufferSolutionName,
                out var buffer,
                out var bufferSolution))
        {
            return;
        }

        if (fromBuffer) // Buffer to container
        {
            amount = FixedPoint2.Min(amount, containerSolution.AvailableVolume);
            amount = bufferSolution.RemoveReagent(id, amount, preserveOrder: true);
            SolContainer.TryAddReagent(containerSoln.Value, id, amount, out _);
        }
        else // Container to buffer
        {
            amount = FixedPoint2.Min(amount, containerSolution.GetReagentQuantity(id));
            SolContainer.RemoveReagent(containerSoln.Value, id, amount);
            bufferSolution.AddReagent(id, amount);
        }

        // AddReagent/RemoveReagent don't auto dirty. :(
        Dirty(containerSoln.Value);
        Dirty(buffer.Value);
        DirtyUI(chemMaster);
    }

    private void DiscardReagents(Entity<ChemMasterComponent> chemMaster, ReagentId id, FixedPoint2 amount, bool fromBuffer)
    {
        if (fromBuffer)
        {
            if (SolContainer.TryGetSolution(chemMaster.Owner, ChemMasterComponent.BufferSolutionName, out var bufferEnt, out var bufferSolution))
            {
                bufferSolution.RemoveReagent(id, amount, preserveOrder: true);
                Dirty(bufferEnt.Value);
            }
        }
        else
        {
            var container = ItemSlots.GetItemOrNull(chemMaster, ChemMasterComponent.InputSlotName);
            if (container is not null &&
                SolContainer.TryGetFitsInDispenser(container.Value, out var containerSolution, out _))
            {
                // I feel like this should maybe be using split but I can't think of any problem with not
                SolContainer.RemoveReagent(containerSolution.Value, id, amount);
                Dirty(containerSolution.Value);
            }
        }

        DirtyUI(chemMaster);
    }

    private void OnCreatePillsMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterCreatePillsMessage message)
    {
        var user = message.Actor;
        var maybeContainer = ItemSlots.GetItemOrNull(chemMaster, ChemMasterComponent.OutputSlotName);
        if (maybeContainer is not { Valid: true } container
            || !TryComp(container, out StorageComponent? storage))
        {
            return; // output can't fit pills
        }

        // Ensure the number is valid.
        if (message.Number == 0 || !Storage.HasSpace((container, storage)))
            return;

        // Ensure the amount is valid.
        if (message.Dosage == 0 || message.Dosage > chemMaster.Comp.PillDosageLimit)
            return;

        // Ensure label length is within the character limit.
        if (message.Label.Length > ChemMasterComponent.LabelMaxLength)
            return;

        var needed = message.Dosage * message.Number;
        if (!WithdrawFromBuffer(chemMaster, needed, user, out var withdrawal))
            return;

        _labelSystem.Label(container, message.Label);
        ClickSound(chemMaster, message.Actor);

        // TODO FIXME ETC BEFORE MERGE
        // This doesn't seem to work: it turns out we can't predict all of pill creation properly
        // because we can't predict creation of solution entities (internally it uses spawn uninitialized)
        // I'm trying to just make that part not predicted but for some reason after pill creation
        // the chemmaster needs a dirty (e.g., from console/vvwrite/clicking a pill type button) to
        // refresh and show the pills.
        Dirty(container, storage);
        Dirty(chemMaster);

        if (_netMan.IsClient)
            return;

        for (var i = 0; i < message.Number; i++)
        {
            var item = PredictedSpawnAttachedTo(PillPrototypeId, Transform(container).Coordinates);
            Storage.Insert(container, item, out _, user: user, storage);
            _labelSystem.Label(item, message.Label);

            SolContainer.EnsureSolutionEntity(item,
                ChemMasterComponent.PillSolutionName,
                out var itemSolution,
                message.Dosage);

            var pill = EnsureComp<PillComponent>(item);
            pill.PillType = chemMaster.Comp.PillType;
            Dirty(item, pill);

            // No predicted solution entity creations yet, we can just spawn the pills
            // (Or we're server and something worse has happened)
            if (!itemSolution.HasValue)
                continue;

            SolContainer.TryAddSolution(itemSolution.Value, withdrawal.SplitSolution(message.Dosage));

            // Log pill creation by a user
            _adminLogger.Add(LogType.Action,
                LogImpact.Low,
                $"{ToPrettyString(user):user} printed {ToPrettyString(item):pill} {SharedSolutionContainerSystem.ToPrettyString(itemSolution.Value.Comp.Solution)}");
        }
    }

    private void OnOutputToBottleMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterOutputToBottleMessage message)
    {
        var user = message.Actor;
        var maybeContainer = ItemSlots.GetItemOrNull(chemMaster, ChemMasterComponent.OutputSlotName);
        if (maybeContainer is not { Valid: true } container
            || !SolContainer.TryGetSolution(container, ChemMasterComponent.BottleSolutionName, out var soln, out var solution))
        {
            return; // output can't fit reagents
        }

        // Ensure the amount is valid.
        if (message.Dosage == 0 || message.Dosage > solution.AvailableVolume)
            return;

        // Ensure label length is within the character limit.
        if (message.Label.Length > ChemMasterComponent.LabelMaxLength)
            return;

        if (!WithdrawFromBuffer(chemMaster, message.Dosage, user, out var withdrawal))
            return;

        _labelSystem.Label(container, message.Label);
        SolContainer.TryAddSolution(soln.Value, withdrawal);

        // Log bottle creation by a user
        _adminLogger.Add(LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(user):user} bottled {ToPrettyString(container):bottle} {SharedSolutionContainerSystem.ToPrettyString(solution)}");

        DirtyUI(chemMaster);
        ClickSound(chemMaster, message.Actor);
    }

    private bool WithdrawFromBuffer(
        Entity<ChemMasterComponent> chemMaster,
        FixedPoint2 neededVolume, EntityUid? user,
        [NotNullWhen(returnValue: true)] out Solution? outputSolution)
    {
        outputSolution = null;

        if (!SolContainer.TryGetSolution(chemMaster.Owner,
                ChemMasterComponent.BufferSolutionName,
                out var bufferEnt,
                out var solution))
            return false;

        if (solution.Volume == 0)
        {
            if (user.HasValue)
                _popupSystem.PopupPredictedCursor(Loc.GetString("chem-master-window-buffer-empty-text"), user.Value);
            return false;
        }

        // ReSharper disable once InvertIf
        if (neededVolume > solution.Volume)
        {
            if (user.HasValue)
                _popupSystem.PopupPredictedCursor(Loc.GetString("chem-master-window-buffer-low-text"), user.Value);
            return false;
        }

        outputSolution = solution.SplitSolution(neededVolume);
        Dirty(bufferEnt.Value);
        return true;
    }

    private void ClickSound(Entity<ChemMasterComponent> chemMaster, EntityUid actor)
    {
        _audioSystem.PlayPredicted(chemMaster.Comp.ClickSound, chemMaster, actor);
    }
}
