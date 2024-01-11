using Content.Server.Administration.Logs;
using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.Nutrition.Components;
using Content.Server.Nutrition.EntitySystems;
using Content.Server.Labels.Components;
using Content.Server.Chemistry;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Dispenser;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using System.Linq;

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
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly SolutionTransferSystem _solutionTransferSystem = default!;
        [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly OpenableSystem _openable = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ReagentDispenserComponent, ComponentStartup>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ReagentDispenserComponent, SolutionContainerChangedEvent>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ReagentDispenserComponent, EntInsertedIntoContainerMessage>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ReagentDispenserComponent, EntRemovedFromContainerMessage>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ReagentDispenserComponent, BoundUIOpenedEvent>(SubscribeUpdateUiState);

            SubscribeLocalEvent<ReagentDispenserComponent, ReagentDispenserSetDispenseAmountMessage>(OnSetDispenseAmountMessage);
            SubscribeLocalEvent<ReagentDispenserComponent, ReagentDispenserDispenseReagentMessage>(OnDispenseReagentMessage);
            SubscribeLocalEvent<ReagentDispenserComponent, ReagentDispenserClearContainerSolutionMessage>(OnClearContainerSolutionMessage);

            SubscribeLocalEvent<ReagentDispenserComponent, MapInitEvent>(OnMapInit);
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

            var state = new ReagentDispenserBoundUserInterfaceState(outputContainerInfo, inventory, reagentDispenser.Comp.DispenseAmount);
            _userInterfaceSystem.TrySetUiState(reagentDispenser, ReagentDispenserUiKey.Key, state);
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

        private List<KeyValuePair<string, KeyValuePair<string, string>>> GetInventory(ReagentDispenserComponent reagentDispenser)
        {
            var inventory = new List<KeyValuePair<string, KeyValuePair<string, string>>>();

            for (var i = 0; i < reagentDispenser.NumSlots; i++)
            {
                var storageSlotId = ReagentDispenserComponent.BaseStorageSlotId + i;
                var storedContainer = _itemSlotsSystem.GetItemOrNull(reagentDispenser.Owner, storageSlotId);

                // Set label from manually-applied label, or metadata if unavailable
                string reagentLabel;
                if (EntityManager.TryGetComponent<LabelComponent?>(storedContainer, out var label) && label.CurrentLabel != null)
                    reagentLabel = label.CurrentLabel;
                else if (EntityManager.TryGetComponent<MetaDataComponent?>(storedContainer, out var metadata))
                    reagentLabel = metadata.EntityName;
                else
                    continue;

                // Add volume remaining label
                FixedPoint2 quantity = 0f;
                if (storedContainer != null && _solutionContainerSystem.TryGetDrainableSolution(storedContainer.Value, out _, out var sol))
                {
                    quantity = sol.Volume;
                }
                var storedAmount = $"({quantity}u)";

                inventory.Add(new KeyValuePair<string, KeyValuePair<string, string>>(storageSlotId, new KeyValuePair<string, string>(reagentLabel, storedAmount)));
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
            // Ensure that the reagent is something this reagent dispenser can dispense.
            var storedContainer = _itemSlotsSystem.GetItemOrNull(reagentDispenser, message.SlotId);
            if (storedContainer == null)
                return;

            var outputContainer = _itemSlotsSystem.GetItemOrNull(reagentDispenser, SharedReagentDispenser.OutputSlotName);
            if (outputContainer is not { Valid: true } || !_solutionContainerSystem.TryGetFitsInDispenser(outputContainer.Value, out var solution, out _))
                return;

            if (_solutionContainerSystem.TryGetDrainableSolution(storedContainer.Value, out var src, out _) &&
                _solutionContainerSystem.TryGetRefillableSolution(outputContainer.Value, out var dst, out _))
            {
                // force open container, if applicable, to avoid confusing people on why it doesn't dispense
                _openable.SetOpen(storedContainer.Value, true);
                _solutionTransferSystem.Transfer(reagentDispenser,
                        storedContainer.Value, src.Value,
                        outputContainer.Value, dst.Value,
                        (int)reagentDispenser.Comp.DispenseAmount);
            }

            UpdateUiState(reagentDispenser);
            ClickSound(reagentDispenser);
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

        private void OnMapInit(EntityUid uid, ReagentDispenserComponent component, MapInitEvent args)
        {
            // Get list of pre-loaded containers
            List<string> preLoad = new List<string>();
            List<string> preLoadLabels = new List<string>();
            if (component.PackPrototypeId is not null
                && _prototypeManager.TryIndex(component.PackPrototypeId, out ReagentDispenserInventoryPrototype? packPrototype))
            {
                preLoad.AddRange(packPrototype.Inventory);
                preLoadLabels.AddRange(packPrototype.Labels);
            }

            // Populate storage slots with base storage slot whitelist
            for (var i = 0; i < component.NumSlots; i++)
            {
                var storageSlotId = ReagentDispenserComponent.BaseStorageSlotId + i;
                ItemSlot storageComponent = new();
                storageComponent.Whitelist = component.StorageWhitelist;
                storageComponent.Swap = false;
                storageComponent.EjectOnBreak = true;

                // Check corresponding index in pre-loaded container (if exists) and set starting item
                if (i < preLoad.Count)
                    storageComponent.StartingItem = preLoad[i];

                component.StorageSlotIds.Add(storageSlotId);
                component.StorageSlots.Add(storageComponent);
                component.StorageSlots[i].Name = "Storage Slot " + (i+1);
                _itemSlotsSystem.AddItemSlot(uid, component.StorageSlotIds[i], component.StorageSlots[i]);

                // Re-label item for brevity (if labels provided)
                if (i < preLoadLabels.Count) {
                    var storedContainer = _itemSlotsSystem.GetItemOrNull(component.Owner, storageSlotId);
                    if (storedContainer != null)
                    {
                        var label = EnsureComp<LabelComponent>(storedContainer.Value);
                        label.CurrentLabel = preLoadLabels[i];
                    }
                }
            }

            _itemSlotsSystem.AddItemSlot(uid, ReagentDispenserComponent.BeakerSlotId, component.BeakerSlot);
        }
    }
}
