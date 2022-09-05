using Content.Server.Chemistry.Components;
using Content.Server.Labels.Components;
using Content.Server.Popups;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Player;


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
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ChemMasterComponent, ComponentStartup>((_, comp, _) => UpdateUiState(comp));
            SubscribeLocalEvent<ChemMasterComponent, SolutionChangedEvent>((_, comp, _) => UpdateUiState(comp));
            SubscribeLocalEvent<ChemMasterComponent, EntInsertedIntoContainerMessage>((_, comp, _) => UpdateUiState(comp));
            SubscribeLocalEvent<ChemMasterComponent, EntRemovedFromContainerMessage>((_, comp, _) => UpdateUiState(comp));
            SubscribeLocalEvent<ChemMasterComponent, BoundUIOpenedEvent>((_, comp, _) => UpdateUiState(comp));

            SubscribeLocalEvent<ChemMasterComponent, ChemMasterSetModeMessage>(OnSetModeMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterSetPillTypeMessage>(OnSetPillTypeMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterReagentAmountButtonMessage>(OnReagentButtonMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterCreatePillsMessage>(OnCreatePillsMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterCreateBottlesMessage>(OnCreateBottlesMessage);
        }

        private void UpdateUiState(ChemMasterComponent chemMaster)
        {
            var bufferSolution = _solutionContainerSystem.EnsureSolution(chemMaster.Owner, ChemMasterComponent.SolutionName);
            var containerSlot = CompOrNull<ItemSlotsComponent>(chemMaster.Owner)?.Slots.GetValueOrDefault("container_slot");
            EntityUid? container = containerSlot?.Item;
            Solution? containerSolution = null;

            if (container.HasValue && container.Value.Valid)
            {
                TryComp(container, out FitsInDispenserComponent? fits);
                _solutionContainerSystem.TryGetSolution(container.Value, fits!.Solution, out containerSolution);
            }

            var containerName = CompOrNull<MetaDataComponent>(container)?.EntityName;
            var dispenserName = CompOrNull<MetaDataComponent>(chemMaster.Owner)?.EntityName ?? "";
            var bufferReagents = bufferSolution.Contents;
            var bufferCurrentVolume = bufferSolution.CurrentVolume;

            var state = new ChemMasterBoundUserInterfaceState(
                containerSolution?.CurrentVolume, containerSolution?.MaxVolume, containerName, chemMaster.Label, dispenserName,
                containerSolution?.Contents, bufferReagents, chemMaster.Mode, bufferCurrentVolume, chemMaster.PillType,
                chemMaster.PillProductionLimit, chemMaster.BottleProductionLimit
            );
            _userInterfaceSystem.TrySetUiState(chemMaster.Owner, ChemMasterUiKey.Key, state);
        }

        private void OnSetModeMessage(EntityUid uid, ChemMasterComponent chemMaster, ChemMasterSetModeMessage message)
        {
            // Ensure the mode is valid, either Transfer or Discard.
            if (!Enum.IsDefined(typeof(ChemMasterMode), message.ChemMasterMode))
                return;

            chemMaster.Mode = message.ChemMasterMode;
            UpdateUiState(chemMaster);
            ClickSound(chemMaster);
        }

        private void OnSetPillTypeMessage(EntityUid uid, ChemMasterComponent chemMaster, ChemMasterSetPillTypeMessage message)
        {
            // Ensure valid pill type. There are 20 pills selectable, 0-19.
            if (message.PillType > 19)
                return;

            chemMaster.PillType = message.PillType;
            UpdateUiState(chemMaster);
            ClickSound(chemMaster);
        }

        private void OnReagentButtonMessage(EntityUid uid, ChemMasterComponent chemMaster, ChemMasterReagentAmountButtonMessage message)
        {
            // Ensure the amount corresponds to one of the reagent amount buttons.
            if (!Enum.IsDefined(typeof(ChemMasterReagentAmount), message.Amount))
                return;

            switch (chemMaster.Mode)
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

            ClickSound(chemMaster);
        }

        private void TransferReagents(ChemMasterComponent chemMaster, string reagentId, FixedPoint2 amount, bool fromBuffer)
        {
            var containerSlot = CompOrNull<ItemSlotsComponent>(chemMaster.Owner)?.Slots.GetValueOrDefault("container_slot");
            if (containerSlot is null)
                return;

            if (!containerSlot.HasItem ||
                containerSlot.Item is not {Valid: true} container ||
                !TryComp(container, out FitsInDispenserComponent? fits) ||
                !_solutionContainerSystem.TryGetSolution(container, fits.Solution, out var containerSolution)
            )
                return;

            var bufferSolution = _solutionContainerSystem.EnsureSolution(chemMaster.Owner, ChemMasterComponent.SolutionName);

            if (fromBuffer) // Buffer to container
            {
                amount = FixedPoint2.Min(amount, containerSolution.AvailableVolume);
                amount = bufferSolution.RemoveReagent(reagentId, amount);
                _solutionContainerSystem.TryAddReagent(container, containerSolution, reagentId, amount, out var _);
            }
            else // Container to buffer
            {
                amount = FixedPoint2.Min(amount, containerSolution.GetReagentQuantity(reagentId));
                _solutionContainerSystem.TryRemoveReagent(container, containerSolution, reagentId, amount);
                bufferSolution.AddReagent(reagentId, amount);
            }

            chemMaster.Label = GenerateLabel(chemMaster);
            UpdateUiState(chemMaster);
        }

        private void DiscardReagents(ChemMasterComponent chemMaster, string reagentId, FixedPoint2 amount, bool fromBuffer)
        {
            var bufferSolution = _solutionContainerSystem.EnsureSolution(chemMaster.Owner, ChemMasterComponent.SolutionName);
            var containerSlot = CompOrNull<ItemSlotsComponent>(chemMaster.Owner)?.Slots.GetValueOrDefault("container_slot");

            if (fromBuffer)
                bufferSolution.RemoveReagent(reagentId, amount);
            else if (containerSlot is not null &&
                containerSlot.HasItem &&
                containerSlot.Item is {Valid: true} container &&
                TryComp(container, out FitsInDispenserComponent? fits) &&
                _solutionContainerSystem.TryGetSolution(container, fits.Solution, out var containerSolution))
            {
                _solutionContainerSystem.TryRemoveReagent(container, containerSolution, reagentId, amount);
            }
            UpdateUiState(chemMaster);
        }

        private void OnCreatePillsMessage(EntityUid uid, ChemMasterComponent chemMaster, ChemMasterCreatePillsMessage message)
        {
            // Ensure the amount is valid.
            if (message.Amount == 0 || message.Amount > chemMaster.PillProductionLimit)
                return;

            CreatePillsOrBottles(chemMaster, pills: true, message.Amount, message.Label, message.Session.AttachedEntity);
            UpdateUiState(chemMaster);
            ClickSound(chemMaster);
        }

        private void OnCreateBottlesMessage(EntityUid uid, ChemMasterComponent chemMaster, ChemMasterCreateBottlesMessage message)
        {
            // Ensure the amount is valid.
            if (message.Amount == 0 || message.Amount > chemMaster.BottleProductionLimit)
                return;

            CreatePillsOrBottles(chemMaster, pills: false, message.Amount, message.Label, message.Session.AttachedEntity);
            UpdateUiState(chemMaster);
            ClickSound(chemMaster);
        }

        private void CreatePillsOrBottles(ChemMasterComponent chemMaster, bool pills, FixedPoint2 amount, string label, EntityUid? user)
        {
            var bufferSolution = _solutionContainerSystem.EnsureSolution(chemMaster.Owner, ChemMasterComponent.SolutionName);
            var filter = user.HasValue ? Filter.Entities(user.Value) : Filter.Empty();
            if (bufferSolution.TotalVolume == 0)
            {
                _popupSystem.PopupCursor(Loc.GetString("chem-master-window-buffer-empty-text"), filter);
                return;
            }

            var individualVolume = FixedPoint2.Min(bufferSolution.TotalVolume / amount, FixedPoint2.New(pills ? 50 : 30));
            if (individualVolume < FixedPoint2.New(1))
            {
                _popupSystem.PopupCursor(Loc.GetString("chem-master-window-buffer-low-text"), filter);
                return;
            }

            for (int i = 0; i < amount; i++)
            {
                var item = Spawn(pills ? "Pill" : "ChemistryEmptyBottle01", Transform(chemMaster.Owner).Coordinates);

                var labelComponent = item.EnsureComponent<LabelComponent>();
                labelComponent.OriginalName = Name(item);
                string val = Name(item) + $" ({label})";
                Comp<MetaDataComponent>(item).EntityName = val;
                labelComponent.CurrentLabel = label;

                var solution = bufferSolution.SplitSolution(individualVolume);
                var itemSolution = _solutionContainerSystem.EnsureSolution(item, pills ? "food" : "drink");
                _solutionContainerSystem.TryAddSolution(item, itemSolution, solution);

                if (pills)
                    Comp<SpriteComponent>(item).LayerSetState(0, "pill" + (chemMaster.PillType + 1));

                if (user.HasValue)
                    _handsSystem.PickupOrDrop(user, item);
            }

            UpdateUiState(chemMaster);
        }

        private string GenerateLabel(ChemMasterComponent chemMaster)
        {
            var bufferSolution = _solutionContainerSystem.EnsureSolution(chemMaster.Owner, ChemMasterComponent.SolutionName);
            if (bufferSolution == null || bufferSolution.Contents.Count == 0)
                return "";

            bufferSolution.Contents.Sort();
            return bufferSolution.Contents[bufferSolution.Contents.Count - 1].ReagentId;
        }

        private void ClickSound(ChemMasterComponent chemMaster)
        {
            _audioSystem.Play(chemMaster.ClickSound, Filter.Pvs(chemMaster.Owner), chemMaster.Owner, AudioParams.Default.WithVolume(-2f));
        }
    }
}
