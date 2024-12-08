using Content.Server.Chemistry.Components;
using Content.Server.Labels;
using Content.Server.Popups;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Dispenser;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Labels.Components;
using Content.Shared.Mobs;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Storage;
using JetBrains.Annotations;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Server.Chemistry.EntitySystems
{

    /// <summary>
    /// System for any machine that transfers things between jugs and beakers
    /// <seealso cref="SolutionTransferMachineComponent"/>
    /// </summary>
    [UsedImplicitly]
    public sealed class SolutionTransferMachineSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
        [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly SolutionTransferSystem _solutionTransferSystem = default!;
        [Dependency] private readonly OpenableSystem _openable = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SolutionTransferMachineComponent, MapInitEvent>(OnMapInit, before: [typeof(ItemSlotsSystem)]);
        }

        /// <summary>
        /// Allows other systems to check if the transfer is valid without having to do all the work on their end,
        /// and make sure the client didn't tell them to pick a chemical from a jug into another within a dispenser
        /// </summary>
        public bool ValidateTransfer(Entity<SolutionTransferMachineComponent?> machine, string inputSlot, string outputSlot, SolutionTransferMachineRestriction restriction)
        {
            if (!Resolve(machine, ref machine.Comp))
                return false;

            var check = SolutionTransferMachineRestriction.Unrestricted;

            if (machine.Comp.StorageSlotIds.Contains(inputSlot))
                check |= SolutionTransferMachineRestriction.FromStorage;
            if (machine.Comp.DispenserSlotIds.Contains(inputSlot))
                check |= SolutionTransferMachineRestriction.FromDispenser;

            if (machine.Comp.StorageSlotIds.Contains(outputSlot))
                check |= SolutionTransferMachineRestriction.IntoStorage;
            if (machine.Comp.DispenserSlotIds.Contains(outputSlot))
                check |= SolutionTransferMachineRestriction.IntoDispenser;

            return (check & restriction) == 0;
        }

        public bool SolutionTransfer(Entity<SolutionTransferMachineComponent?> machine, string inputSlot, string outputSlot, int amount, ReagentId? reagentFilter = null, bool allowPartial = true)
        {
            if (!Resolve(machine, ref machine.Comp))
                return false;

            // Make sure both input and output containers exist. Not using FitsInDispenser because that should just be filtered for by whitelist

            var inputContainer = _itemSlotsSystem.GetItemOrNull(machine, inputSlot);
            if (inputContainer is not { Valid: true })
                return false;

            var outputContainer = _itemSlotsSystem.GetItemOrNull(machine, outputSlot);
            if (outputContainer is not { Valid: true })
                return false;

            if (!_solutionContainerSystem.TryGetDrainableSolution(inputContainer.Value, out var source, out var sourceSolution) ||
                !_solutionContainerSystem.TryGetRefillableSolution(outputContainer.Value, out var output, out var outputSolution))
                return false;

            // Force open container, if applicable, to avoid confusing people on why it doesn't dispense
            _openable.SetOpen(inputContainer.Value, true);
            _openable.SetOpen(outputContainer.Value, true);

            var possibleAmount = FixedPoint2.Min(
                amount,
                reagentFilter is null ? sourceSolution.Volume : sourceSolution.GetReagentQuantity(reagentFilter.Value),
                outputSolution.AvailableVolume
            );

            // Might want to have some feedback here for other things
            if (possibleAmount == 0)
                return false;

            if (!allowPartial && possibleAmount < amount)
                return false;

            if (reagentFilter is null)
            {
                _solutionTransferSystem.Transfer(machine,
                        inputContainer.Value, source.Value,
                        outputContainer.Value, output.Value,
                        possibleAmount
                );
                return true;
            }

            if (machine.Comp.AllowFiltering)
            {
                DebugTools.Assert("Solution transfer was requested with a filter despite the component not allowing such - disable such functionality if AllowFiltering is false");
                return false;
            }

            // TODO: When Chemistry Refactor #30254 is merged, check if this is ok
            var ignoredReagents = sourceSolution.Contents.Where(r => r.Reagent != reagentFilter.Value).Select(r => r.Reagent.Prototype).ToArray();

            // Make sure to split the solution out and transfer that, don't just delete reagents to recreate them elsewhere
            // Also preserves their temperature, woo
            var isolatedReagentSolution = _solutionContainerSystem.SplitSolutionWithout(source.Value, possibleAmount, ignoredReagents);
            _solutionContainerSystem.TryAddSolution(output.Value, isolatedReagentSolution);

            return true;
        }

        public List<ReagentInventoryItem> GetInventory(Entity<SolutionTransferMachineComponent?> machine, bool dispenserInventory, bool withContents = false, bool fullName = false)
        {
            if (!Resolve(machine, ref machine.Comp))
                return [];

            List<ReagentInventoryItem> inventory = [];

            foreach (var storageSlotId in dispenserInventory ? machine.Comp.DispenserSlotIds : machine.Comp.StorageSlotIds)
            {
                var info = GetSlotInfo(machine, storageSlotId, withContents, fullName);
                if (info is not null)
                    inventory.Add(info);
            }

            return inventory;
        }

        public ReagentInventoryItem? GetSlotInfo(Entity<SolutionTransferMachineComponent?> machine, string slotId, bool withContents, bool fullName)
        {
            if (!Resolve(machine, ref machine.Comp))
                return null;

            var storedContainer = _itemSlotsSystem.GetItemOrNull(machine, slotId);

            // Set label from manually-applied label, or metadata if unavailable
            string displayName;
            if (!fullName && TryComp<LabelComponent>(storedContainer, out var label) && !string.IsNullOrEmpty(label.CurrentLabel))
                displayName = label.CurrentLabel;
            else if (storedContainer != null)
                displayName = Name(storedContainer.Value);
            else
                return null;

            if (storedContainer != null && _solutionContainerSystem.TryGetDrainableSolution(storedContainer.Value, out _, out var sol))
            {
                return new ReagentInventoryItem(
                    slotId, displayName, sol.GetColor(_prototypeManager),
                    sol.Volume, sol.MaxVolume,
                    withContents ? sol.Contents : null,
                    GetNetEntity(storedContainer)
                );
            }

            return new ReagentInventoryItem(
                slotId, displayName, Color.White,
                0, null, null, GetNetEntity(storedContainer)
            );
        }

        /// <summary>
        /// Automatically generate storage slots for all NumSlots, and fill them with their initial chemicals.
        /// The actual spawning of entities happens in ItemSlotsSystem's MapInit.
        /// </summary>
        private void OnMapInit(EntityUid uid, SolutionTransferMachineComponent component, MapInitEvent args)
        {
            // Get list of pre-loaded containers
            List<string> preLoad = [];
            if (component.PackPrototypeId is not null
                && _prototypeManager.TryIndex(component.PackPrototypeId, out ReagentDispenserInventoryPrototype? packPrototype))
            {
                preLoad.AddRange(packPrototype.Inventory);
            }

            // Populate storage slots with base storage slot whitelist
            for (var i = 0; i < component.MaxStorageSlots; i++)
            {
                var storageSlotId = SharedSolutionTransferMachineSystem.BaseStorageSlotId + i;
                ItemSlot storageComponent = new()
                {
                    Whitelist = component.StorageWhitelist,
                    Swap = false,
                    EjectOnBreak = true
                };

                // Check corresponding index in pre-loaded container (if exists) and set starting item
                if (i < preLoad.Count)
                    storageComponent.StartingItem = preLoad[i];

                component.StorageSlotIds.Add(storageSlotId);
                component.StorageSlots.Add(storageComponent);
                component.StorageSlots[i].Name = "Storage Slot " + (i + 1);
                _itemSlotsSystem.AddItemSlot(uid, component.StorageSlotIds[i], component.StorageSlots[i]);
            }

            // Populate storage slots with base storage slot whitelist
            for (var i = 0; i < component.MaxDispenserSlots; i++)
            {
                var storageSlotId = SharedSolutionTransferMachineSystem.BaseDispenserSlotId + i;
                ItemSlot storageComponent = new()
                {
                    //WhitelistFailPopup = "reagent-dispenser-component-cannot-put-entity-message",
                    Whitelist = component.DispenserWhitelist,
                    Swap = true,
                    EjectOnBreak = true
                };

                component.DispenserSlotIds.Add(storageSlotId);
                component.DispenserContainerSlots.Add(storageComponent);
                component.DispenserContainerSlots[i].Name = "Dispenser Slot " + (i + 1);
                _itemSlotsSystem.AddItemSlot(uid, component.DispenserSlotIds[i], component.DispenserContainerSlots[i]);
            }
        }
    }
}