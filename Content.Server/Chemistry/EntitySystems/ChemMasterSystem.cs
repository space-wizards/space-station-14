using Content.Server.Chemistry.Components;
using Content.Server.Labels;
using Content.Server.Popups;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Storage;
using JetBrains.Annotations;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Server.Chemistry.EntitySystems
{

    /// <summary>
    /// Contains all the server-side logic for ChemMasters.
    /// <seealso cref="ChemMasterComponent"/>
    /// </summary>
    [UsedImplicitly]
    public sealed class ChemMasterSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly AudioSystem _audioSystem = default!;
        [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private readonly StorageSystem _storageSystem = default!;
        [Dependency] private readonly LabelSystem _labelSystem = default!;
        [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

        [ValidatePrototypeId<EntityPrototype>]
        private const string PillPrototypeId = "Pill";

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ChemMasterComponent, ComponentStartup>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ChemMasterComponent, SolutionContainerChangedEvent>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ChemMasterComponent, EntInsertedIntoContainerMessage>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ChemMasterComponent, EntRemovedFromContainerMessage>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ChemMasterComponent, BoundUIOpenedEvent>(SubscribeUpdateUiState);

            SubscribeLocalEvent<ChemMasterComponent, ChemMasterSetModeMessage>(OnSetModeMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterSetPillTypeMessage>(OnSetPillTypeMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterReagentAmountButtonMessage>(OnReagentButtonMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterCreatePillsMessage>(OnCreatePillsMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterOutputToBottleMessage>(OnOutputToBottleMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterDiscardBufferMessage>(OnDiscardBufferMessage);
        }

        private void SubscribeUpdateUiState<T>(Entity<ChemMasterComponent> ent, ref T ev)
        {
            UpdateUiState(ent);
        }

        private void UpdateUiState(Entity<ChemMasterComponent> ent, bool updateLabel = false)
        {
            var (owner, chemMaster) = ent;
            if (!_solutionContainerSystem.TryGetSolution(owner, SharedChemMaster.StorageBufferSolutionName, out _, out var bufferStorageSolution))
                return;
            if (!_solutionContainerSystem.TryGetSolution(owner, SharedChemMaster.OutputBufferSolutionName, out _, out var bufferOutputSolution))
                return;
            var inputContainer = _itemSlotsSystem.GetItemOrNull(owner, SharedChemMaster.InputSlotName);
            var outputContainer = _itemSlotsSystem.GetItemOrNull(owner, SharedChemMaster.OutputSlotName);

            var bufferStorageReagents = bufferStorageSolution.Contents;
            var bufferStorageVolume = bufferStorageSolution.Volume;

            var bufferOutputReagents = bufferOutputSolution.Contents;
            var bufferOutputVolume = bufferOutputSolution.Volume;

            var state = new ChemMasterBoundUserInterfaceState(
                chemMaster.Mode, BuildInputContainerInfo(inputContainer), BuildOutputContainerInfo(outputContainer),
                bufferStorageReagents, bufferOutputReagents, bufferStorageVolume, bufferOutputVolume, chemMaster.PillType, chemMaster.PillDosageLimit, updateLabel);

            _userInterfaceSystem.SetUiState(owner, ChemMasterUiKey.Key, state);
        }

        private void OnSetModeMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterSetModeMessage message)
        {
            // Ensure the mode is valid, either Transfer or Discard.
            if (!Enum.IsDefined(typeof(ChemMasterMode), message.ChemMasterMode))
                return;

            chemMaster.Comp.Mode = message.ChemMasterMode;
            UpdateUiState(chemMaster);
            ClickSound(chemMaster);
        }

        private void OnSetPillTypeMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterSetPillTypeMessage message)
        {
            // Ensure valid pill type. There are 20 pills selectable, 0-19.
            if (message.PillType > SharedChemMaster.PillTypes - 1)
                return;

            chemMaster.Comp.PillType = message.PillType;
            UpdateUiState(chemMaster);
            ClickSound(chemMaster);
        }

        private void OnReagentButtonMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterReagentAmountButtonMessage message)
        {
            // Ensure the amount corresponds to one of the reagent amount buttons.
            if (!Enum.IsDefined(typeof(ChemMasterReagentAmount), message.Amount))
                return;

            switch (chemMaster.Comp.Mode)
            {
                case ChemMasterMode.Storage:
                    TransferReagents(chemMaster, message.ReagentId, message.Amount.GetFixedPoint(), message.FromBuffer, message.Origin);
                    break;
                case ChemMasterMode.Output:
                    TransferReagents(chemMaster, message.ReagentId, message.Amount.GetFixedPoint(), message.FromBuffer, message.Origin);
                    break;
                case ChemMasterMode.Discard:
                    DiscardReagents(chemMaster, message.ReagentId, message.Amount.GetFixedPoint(), message.FromBuffer, SharedChemMaster.StorageBufferSolutionName);
                    break;
                default:
                    // Invalid mode.
                    return;
            }

            ClickSound(chemMaster);
        }

        public void OnDiscardBufferMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterDiscardBufferMessage message)
        {
            if (!_solutionContainerSystem.TryGetSolution(chemMaster.Owner, message.BufferName, out var containerSolution, out var bufferSolution))
            {
                return;
            }

            _solutionContainerSystem.RemoveAllSolution(containerSolution.Value);

            UpdateUiState(chemMaster, updateLabel: true);
            ClickSound(chemMaster);
        }

        // Transfer reagents from the container to one of the buffers, or between buffers depending on the mode and origin.
        // Output buffer can't transfer back to container, only to storage buffer.
        private void TransferReagents(Entity<ChemMasterComponent> chemMaster, ReagentId id, FixedPoint2 amount, bool fromBuffer, string origin)
        {
            // Input Slot to Storage Buffer
            if (origin == SharedChemMaster.InputSlotName && chemMaster.Comp.Mode == ChemMasterMode.Storage)
            {
                var container = _itemSlotsSystem.GetItemOrNull(chemMaster, SharedChemMaster.InputSlotName);
                if (container is null ||
                    !_solutionContainerSystem.TryGetFitsInDispenser(container.Value, out var containerSoln, out var containerSolution) ||
                    !_solutionContainerSystem.TryGetSolution(chemMaster.Owner, SharedChemMaster.StorageBufferSolutionName, out var _, out var bufferSolution))
                {
                    return;
                }

                amount = FixedPoint2.Min(amount, containerSolution.GetReagentQuantity(id));
                _solutionContainerSystem.RemoveReagent(containerSoln.Value, id, amount);
                bufferSolution.AddReagent(id, amount);
            }
            // Storage Buffer to Input Slot
            else if (origin == SharedChemMaster.StorageBufferSolutionName && chemMaster.Comp.Mode == ChemMasterMode.Storage)
            {
                var container = _itemSlotsSystem.GetItemOrNull(chemMaster, SharedChemMaster.InputSlotName);
                if (container is null ||
                    !_solutionContainerSystem.TryGetFitsInDispenser(container.Value, out var containerSoln, out var containerSolution) ||
                    !_solutionContainerSystem.TryGetSolution(chemMaster.Owner, SharedChemMaster.StorageBufferSolutionName, out var _, out var bufferSolution))
                {
                    return;
                }

                amount = FixedPoint2.Min(amount, bufferSolution.GetReagentQuantity(id));
                bufferSolution.RemoveReagent(id, amount);
                _solutionContainerSystem.TryAddReagent(containerSoln.Value, id, amount, out var _);

            }
            // Input Slot to Output Buffer
            else if (origin == SharedChemMaster.InputSlotName && chemMaster.Comp.Mode == ChemMasterMode.Output)
            {
                var container = _itemSlotsSystem.GetItemOrNull(chemMaster, SharedChemMaster.InputSlotName);
                if (container is null ||
                    !_solutionContainerSystem.TryGetFitsInDispenser(container.Value, out var containerSoln, out var containerSolution) ||
                    !_solutionContainerSystem.TryGetSolution(chemMaster.Owner, SharedChemMaster.OutputBufferSolutionName, out var _, out var bufferSolution))
                {
                    return;
                }

                amount = FixedPoint2.Min(amount, containerSolution.GetReagentQuantity(id));
                _solutionContainerSystem.RemoveReagent(containerSoln.Value, id, amount);
                bufferSolution.AddReagent(id, amount);
            }
            // Storage Buffer to Output Buffer
            else if (origin == SharedChemMaster.StorageBufferSolutionName && chemMaster.Comp.Mode == ChemMasterMode.Output)
            {
                if (!_solutionContainerSystem.TryGetSolution(chemMaster.Owner, SharedChemMaster.StorageBufferSolutionName, out var _, out var storageBufferSolution) ||
                    !_solutionContainerSystem.TryGetSolution(chemMaster.Owner, SharedChemMaster.OutputBufferSolutionName, out var _, out var outputBufferSolution))
                {
                    return;
                }

                amount = FixedPoint2.Min(amount, storageBufferSolution.GetReagentQuantity(id));
                storageBufferSolution.RemoveReagent(id, amount);
                outputBufferSolution.AddReagent(id, amount);
            }
            // Output buffer to Storage Buffer
            else if (origin == SharedChemMaster.OutputBufferSolutionName)
            {
                if (!_solutionContainerSystem.TryGetSolution(chemMaster.Owner, SharedChemMaster.OutputBufferSolutionName, out var _, out var outputBufferSolution) ||
                    !_solutionContainerSystem.TryGetSolution(chemMaster.Owner, SharedChemMaster.StorageBufferSolutionName, out var _, out var storageBufferSolution))
                {
                    return;
                }

                amount = FixedPoint2.Min(amount, outputBufferSolution.GetReagentQuantity(id));
                outputBufferSolution.RemoveReagent(id, amount, preserveOrder: true);
                storageBufferSolution.AddReagent(id, amount);
            }
            else
            {
                return;
            }

            UpdateUiState(chemMaster, updateLabel: true);
        }

        private void DiscardReagents(Entity<ChemMasterComponent> chemMaster, ReagentId id, FixedPoint2 amount, bool fromBuffer, string bufferName)
        {
            if (fromBuffer)
            {
                if (_solutionContainerSystem.TryGetSolution(chemMaster.Owner, bufferName, out _, out var bufferStorageSolution))
                    bufferStorageSolution.RemoveReagent(id, amount, preserveOrder: true);
                else
                    return;
            }
            else
            {
                var container = _itemSlotsSystem.GetItemOrNull(chemMaster, SharedChemMaster.InputSlotName);
                if (container is not null &&
                    _solutionContainerSystem.TryGetFitsInDispenser(container.Value, out var containerSolution, out _))
                {
                    _solutionContainerSystem.RemoveReagent(containerSolution.Value, id, amount);
                }
                else
                    return;
            }

            UpdateUiState(chemMaster, updateLabel: fromBuffer);
        }

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
            if (!WithdrawFromBuffer(chemMaster, needed, user, SharedChemMaster.OutputBufferSolutionName, out var withdrawal))
                return;

            _labelSystem.Label(container, message.Label);

            for (var i = 0; i < message.Number; i++)
            {
                var item = Spawn(PillPrototypeId, Transform(container).Coordinates);
                _storageSystem.Insert(container, item, out _, user: user, storage);
                _labelSystem.Label(item, message.Label);

                _solutionContainerSystem.EnsureSolutionEntity(item, SharedChemMaster.PillSolutionName,out var itemSolution ,message.Dosage);
                if (!itemSolution.HasValue)
                    return;

                _solutionContainerSystem.TryAddSolution(itemSolution.Value, withdrawal.SplitSolution(message.Dosage));

                var pill = EnsureComp<PillComponent>(item);
                pill.PillType = chemMaster.Comp.PillType;
                Dirty(item, pill);

                // Log pill creation by a user
                _adminLogger.Add(LogType.Action, LogImpact.Low,
                    $"{ToPrettyString(user):user} printed {ToPrettyString(item):pill} {SharedSolutionContainerSystem.ToPrettyString(itemSolution.Value.Comp.Solution)}");
            }

            UpdateUiState(chemMaster);
            ClickSound(chemMaster);
        }

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

            if (!WithdrawFromBuffer(chemMaster, message.Dosage, user, SharedChemMaster.OutputBufferSolutionName, out var withdrawal))
                return;

            _labelSystem.Label(container, message.Label);
            _solutionContainerSystem.TryAddSolution(soln.Value, withdrawal);

            // Log bottle creation by a user
            _adminLogger.Add(LogType.Action, LogImpact.Low,
                $"{ToPrettyString(user):user} bottled {ToPrettyString(container):bottle} {SharedSolutionContainerSystem.ToPrettyString(solution)}");

            UpdateUiState(chemMaster);
            ClickSound(chemMaster);
        }

        private bool WithdrawFromBuffer(
            Entity<ChemMasterComponent> chemMaster,
            FixedPoint2 neededVolume, EntityUid? user,
            string bufferName,
            [NotNullWhen(returnValue: true)] out Solution? outputSolution)
        {
            outputSolution = null;

            if (!_solutionContainerSystem.TryGetSolution(chemMaster.Owner, bufferName, out _, out var solution))
            {
                return false;
            }

            if (solution.Volume == 0)
            {
                if (user.HasValue)
                    _popupSystem.PopupCursor(Loc.GetString("chem-master-window-buffer-empty-text"), user.Value);
                return false;
            }

            // ReSharper disable once InvertIf
            if (neededVolume > solution.Volume)
            {
                if (user.HasValue)
                    _popupSystem.PopupCursor(Loc.GetString("chem-master-window-buffer-low-text"), user.Value);
                return false;
            }

            outputSolution = solution.SplitSolution(neededVolume);
            return true;
        }

        private void ClickSound(Entity<ChemMasterComponent> chemMaster)
        {
            _audioSystem.PlayPvs(chemMaster.Comp.ClickSound, chemMaster, AudioParams.Default.WithVolume(-2f));
        }

        private ContainerInfo? BuildInputContainerInfo(EntityUid? container)
        {
            if (container is not { Valid: true })
                return null;

            if (!TryComp(container, out FitsInDispenserComponent? fits)
                || !_solutionContainerSystem.TryGetSolution(container.Value, fits.Solution, out _, out var solution))
            {
                return null;
            }

            return BuildContainerInfo(Name(container.Value), solution);
        }

        private ContainerInfo? BuildOutputContainerInfo(EntityUid? container)
        {
            if (container is not { Valid: true })
                return null;

            var name = Name(container.Value);
            {
                if (_solutionContainerSystem.TryGetSolution(
                        container.Value, SharedChemMaster.BottleSolutionName, out _, out var solution))
                {
                    return BuildContainerInfo(name, solution);
                }
            }

            if (!TryComp(container, out StorageComponent? storage))
                return null;

            var pills = storage.Container.ContainedEntities.Select((Func<EntityUid, (string, FixedPoint2 quantity)>) (pill =>
            {
                _solutionContainerSystem.TryGetSolution(pill, SharedChemMaster.PillSolutionName, out _, out var solution);
                var quantity = solution?.Volume ?? FixedPoint2.Zero;
                return (Name(pill), quantity);
            })).ToList();

            return new ContainerInfo(name, _storageSystem.GetCumulativeItemAreas((container.Value, storage)), storage.Grid.GetArea())
            {
                Entities = pills
            };
        }

        private static ContainerInfo BuildContainerInfo(string name, Solution solution)
        {
            return new ContainerInfo(name, solution.Volume, solution.MaxVolume)
            {
                Reagents = solution.Contents
            };
        }
    }
}
