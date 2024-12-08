using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Dispenser;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using Content.Shared.Nutrition.EntitySystems;
using JetBrains.Annotations;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Content.Shared.Labels.Components;

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
        [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly SolutionTransferMachineSystem _transferMachineSystem = default!;

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
        }

        private void SubscribeUpdateUiState<T>(Entity<ReagentDispenserComponent> ent, ref T ev)
        {
            UpdateUiState(ent);
        }

        private void UpdateUiState(Entity<ReagentDispenserComponent> reagentDispenser)
        {
            if (!TryComp(reagentDispenser, out SolutionTransferMachineComponent? transferMachine))
                return;

            var dispenserContainers = _transferMachineSystem.GetInventory(new Entity<SolutionTransferMachineComponent?>(reagentDispenser.Owner, transferMachine), dispenserInventory: true, withContents: true, fullName: true);
            var inventory = _transferMachineSystem.GetInventory(new Entity<SolutionTransferMachineComponent?>(reagentDispenser.Owner, transferMachine), dispenserInventory: false);

            var state = new ReagentDispenserBoundUserInterfaceState(
                dispenserContainers.Count > 0 ? dispenserContainers[0] : null,
                inventory,
                reagentDispenser.Comp.DispenseAmount
            );
            _userInterfaceSystem.SetUiState(reagentDispenser.Owner, ReagentDispenserUiKey.Key, state);
        }

        private void OnSetDispenseAmountMessage(Entity<ReagentDispenserComponent> reagentDispenser, ref ReagentDispenserSetDispenseAmountMessage message)
        {
            reagentDispenser.Comp.DispenseAmount = message.ReagentDispenserDispenseAmount;
            UpdateUiState(reagentDispenser);
            ClickSound(reagentDispenser);
        }

        private void OnDispenseReagentMessage(Entity<ReagentDispenserComponent> reagentDispenser, ref ReagentDispenserDispenseReagentMessage message)
        {
            if (!TryComp(reagentDispenser, out SolutionTransferMachineComponent? transferMachine))
                return;

            var machine = new Entity<SolutionTransferMachineComponent?>(reagentDispenser.Owner, transferMachine);

            if (!_transferMachineSystem.ValidateTransfer(
                machine, message.SlotId, transferMachine.DispenserSlotIds[0],
                restriction: SolutionTransferMachineRestriction.IntoStorage
                    | SolutionTransferMachineRestriction.FromDispenser
                    | SolutionTransferMachineRestriction.StoragePicking
                    | SolutionTransferMachineRestriction.DispenserPicking
            ))
                return;

            if (!_transferMachineSystem.SolutionTransfer(machine, message.SlotId, transferMachine.DispenserSlotIds[0], (int)reagentDispenser.Comp.DispenseAmount))
                return;

            UpdateUiState(reagentDispenser);
            ClickSound(reagentDispenser);
        }

        private void OnClearContainerSolutionMessage(Entity<ReagentDispenserComponent> reagentDispenser, ref ReagentDispenserClearContainerSolutionMessage message)
        {
            if (!TryComp(reagentDispenser, out SolutionTransferMachineComponent? transferMachine))
                return;

            var outputContainer = _itemSlotsSystem.GetItemOrNull(reagentDispenser, transferMachine.DispenserSlotIds[0]);
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
    }
}
