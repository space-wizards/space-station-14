using System.Linq;
using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Storage.EntitySystems;
using JetBrains.Annotations;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Content.Shared.Labels.Components;
using Content.Shared.Storage;
using Content.Server.Hands.Systems;

namespace Content.Server.Chemistry.EntitySystems
{
    /// <summary>
    /// Contains all the server-side logic for reagent dispensers.
    /// <seealso cref="ReagentDispenserComponent"/>
    /// </summary>
    [UsedImplicitly]
    public sealed class ReagentDispenserSystem : EntitySystem
    {
        [Dependency] private readonly AudioSystem _audioSystem = default!;
        [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly SolutionTransferSystem _solutionTransferSystem = default!;
        [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly OpenableSystem _openable = default!;
        [Dependency] private readonly HandsSystem _handsSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ReagentDispenserComponent, ComponentStartup>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ReagentDispenserComponent, SolutionContainerChangedEvent>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ReagentDispenserComponent, EntInsertedIntoContainerMessage>(SubscribeUpdateUiState, after: [typeof(SharedStorageSystem)]);
            SubscribeLocalEvent<ReagentDispenserComponent, EntRemovedFromContainerMessage>(SubscribeUpdateUiState, after: [typeof(SharedStorageSystem)]);
            SubscribeLocalEvent<ReagentDispenserComponent, BoundUIOpenedEvent>(SubscribeUpdateUiState);

            SubscribeLocalEvent<ReagentDispenserComponent, ReagentDispenserSetDispenseAmountMessage>(OnSetDispenseAmountMessage);
            SubscribeLocalEvent<ReagentDispenserComponent, ReagentDispenserDispenseReagentMessage>(OnDispenseReagentMessage);
            SubscribeLocalEvent<ReagentDispenserComponent, ReagentDispenserEjectContainerMessage>(OnEjectReagentMessage);
            SubscribeLocalEvent<ReagentDispenserComponent, ReagentDispenserClearContainerSolutionMessage>(OnClearContainerSolutionMessage);

            SubscribeLocalEvent<ReagentDispenserComponent, MapInitEvent>(OnMapInit, before: new[] { typeof(ItemSlotsSystem) });
        }

        private void SubscribeUpdateUiState<T>(Entity<ReagentDispenserComponent> ent, ref T ev)
        {
            UpdateUiState(ent);
        }

        private void UpdateUiState(Entity<ReagentDispenserComponent> reagentDispenser)
        {
            var outputContainer = _itemSlotsSystem.GetItemOrNull(reagentDispenser, SharedReagentDispenser.OutputSlotName);
            var outputContainerInfo = BuildOutputContainerInfo(outputContainer);

            var inventory = GetInventory(reagentDispenser);

            var state = new ReagentDispenserBoundUserInterfaceState(outputContainerInfo, GetNetEntity(outputContainer), inventory, reagentDispenser.Comp.DispenseAmount);
            _userInterfaceSystem.SetUiState(reagentDispenser.Owner, ReagentDispenserUiKey.Key, state);
        }

        private ContainerInfo? BuildOutputContainerInfo(EntityUid? container)
        {
            if (container is not { Valid: true })
                return null;

            if (_solutionContainerSystem.TryGetFitsInDispenser(container.Value, out _, out var solution))
            {
                return new ContainerInfo(Name(container.Value), solution.Volume, solution.MaxVolume)
                {
                    Reagents = solution.Contents
                };
            }

            return null;
        }

        private List<ReagentInventoryItem> GetInventory(Entity<ReagentDispenserComponent> reagentDispenser)
        {
            if (!TryComp<StorageComponent>(reagentDispenser.Owner, out var storage))
            {
                return [];
            }

            var inventory = new List<ReagentInventoryItem>();

            foreach (var (storedContainer, storageLocation) in storage.StoredItems)
            {
                string reagentLabel;
                if (TryComp<LabelComponent>(storedContainer, out var label) && !string.IsNullOrEmpty(label.CurrentLabel))
                    reagentLabel = label.CurrentLabel;
                else
                    reagentLabel = Name(storedContainer);

                // Get volume remaining and color of solution
                FixedPoint2 quantity = 0f;
                var reagentColor = Color.White;
                if (_solutionContainerSystem.TryGetDrainableSolution(storedContainer, out _, out var sol))
                {
                    quantity = sol.Volume;
                    reagentColor = sol.GetColor(_prototypeManager);
                }

                inventory.Add(new ReagentInventoryItem(storageLocation, reagentLabel, quantity, reagentColor));
            }

            return inventory;
        }

        private void OnSetDispenseAmountMessage(Entity<ReagentDispenserComponent> reagentDispenser, ref ReagentDispenserSetDispenseAmountMessage message)
        {
            reagentDispenser.Comp.DispenseAmount = message.ReagentDispenserDispenseAmount;
            UpdateUiState(reagentDispenser);
            ClickSound(reagentDispenser);
        }

        private void OnDispenseReagentMessage(Entity<ReagentDispenserComponent> reagentDispenser, ref ReagentDispenserDispenseReagentMessage message)
        {
            if (!TryComp<StorageComponent>(reagentDispenser.Owner, out var storage))
            {
                return;
            }

            // Ensure that the reagent is something this reagent dispenser can dispense.
            var storageLocation = message.StorageLocation;
            var storedContainer = storage.StoredItems.FirstOrDefault(kvp => kvp.Value == storageLocation).Key;
            if (storedContainer == EntityUid.Invalid)
                return;

            var outputContainer = _itemSlotsSystem.GetItemOrNull(reagentDispenser, SharedReagentDispenser.OutputSlotName);
            if (outputContainer is not { Valid: true } || !_solutionContainerSystem.TryGetFitsInDispenser(outputContainer.Value, out var solution, out _))
                return;

            if (_solutionContainerSystem.TryGetDrainableSolution(storedContainer, out var src, out _) &&
                _solutionContainerSystem.TryGetRefillableSolution(outputContainer.Value, out var dst, out _))
            {
                // force open container, if applicable, to avoid confusing people on why it doesn't dispense
                _openable.SetOpen(storedContainer, true);
                _solutionTransferSystem.Transfer(new SolutionTransferData(reagentDispenser,
                        storedContainer, src.Value,
                        outputContainer.Value, dst.Value,
                        (int)reagentDispenser.Comp.DispenseAmount));
            }

            UpdateUiState(reagentDispenser);
            ClickSound(reagentDispenser);
        }

        private void OnEjectReagentMessage(Entity<ReagentDispenserComponent> reagentDispenser, ref ReagentDispenserEjectContainerMessage message)
        {
            if (!TryComp<StorageComponent>(reagentDispenser.Owner, out var storage))
            {
                return;
            }

            var storageLocation = message.StorageLocation;
            var storedContainer = storage.StoredItems.FirstOrDefault(kvp => kvp.Value == storageLocation).Key;
            if (storedContainer == EntityUid.Invalid)
                return;

            _handsSystem.TryPickupAnyHand(message.Actor, storedContainer);
        }

        private void OnClearContainerSolutionMessage(Entity<ReagentDispenserComponent> reagentDispenser, ref ReagentDispenserClearContainerSolutionMessage message)
        {
            var outputContainer = _itemSlotsSystem.GetItemOrNull(reagentDispenser, SharedReagentDispenser.OutputSlotName);
            if (outputContainer is not { Valid: true } || !_solutionContainerSystem.TryGetFitsInDispenser(outputContainer.Value, out var solution, out _))
                return;

            _solutionContainerSystem.RemoveAllSolution(solution.Value);
            UpdateUiState(reagentDispenser);
            ClickSound(reagentDispenser);
        }

        private void ClickSound(Entity<ReagentDispenserComponent> reagentDispenser)
        {
            _audioSystem.PlayPvs(reagentDispenser.Comp.ClickSound, reagentDispenser, AudioParams.Default.WithVolume(-2f));
        }

        /// <summary>
        /// Initializes the beaker slot
        /// </summary>
        private void OnMapInit(Entity<ReagentDispenserComponent> ent, ref MapInitEvent args)
        {
            _itemSlotsSystem.AddItemSlot(ent.Owner, SharedReagentDispenser.OutputSlotName, ent.Comp.BeakerSlot);
        }
    }
}
