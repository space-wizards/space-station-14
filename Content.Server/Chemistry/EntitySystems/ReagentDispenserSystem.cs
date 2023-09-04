using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Chemistry.Components;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Dispenser;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

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
        [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ReagentDispenserComponent, ComponentStartup>((_, comp, _) => UpdateUiState(comp));
            SubscribeLocalEvent<ReagentDispenserComponent, SolutionChangedEvent>((_, comp, _) => UpdateUiState(comp));
            SubscribeLocalEvent<ReagentDispenserComponent, EntInsertedIntoContainerMessage>((_, comp, _) => UpdateUiState(comp));
            SubscribeLocalEvent<ReagentDispenserComponent, EntRemovedFromContainerMessage>((_, comp, _) => UpdateUiState(comp));
            SubscribeLocalEvent<ReagentDispenserComponent, BoundUIOpenedEvent>((_, comp, _) => UpdateUiState(comp));
            SubscribeLocalEvent<ReagentDispenserComponent, GotEmaggedEvent>(OnEmagged);

            SubscribeLocalEvent<ReagentDispenserComponent, ReagentDispenserSetDispenseAmountMessage>(OnSetDispenseAmountMessage);
            SubscribeLocalEvent<ReagentDispenserComponent, ReagentDispenserDispenseReagentMessage>(OnDispenseReagentMessage);
            SubscribeLocalEvent<ReagentDispenserComponent, ReagentDispenserClearContainerSolutionMessage>(OnClearContainerSolutionMessage);
        }

        private void UpdateUiState(ReagentDispenserComponent reagentDispenser)
        {
            var outputContainer = _itemSlotsSystem.GetItemOrNull(reagentDispenser.Owner, SharedReagentDispenser.OutputSlotName);
            var outputContainerInfo = BuildOutputContainerInfo(outputContainer);

            var inventory = GetInventory(reagentDispenser);

            var state = new ReagentDispenserBoundUserInterfaceState(outputContainerInfo, inventory, reagentDispenser.DispenseAmount);
            _userInterfaceSystem.TrySetUiState(reagentDispenser.Owner, ReagentDispenserUiKey.Key, state);
        }

        private ContainerInfo? BuildOutputContainerInfo(EntityUid? container)
        {
            if (container is not { Valid: true })
                return null;

            if (_solutionContainerSystem.TryGetFitsInDispenser(container.Value, out var solution))
            {
                return new ContainerInfo(Name(container.Value), solution.Volume, solution.MaxVolume)
                {
                    Reagents = solution.Contents
                };
            }

            return null;
        }

        private List<ReagentId> GetInventory(ReagentDispenserComponent reagentDispenser)
        {
            var inventory = new List<ReagentId>();

            if (reagentDispenser.PackPrototypeId is not null
                && _prototypeManager.TryIndex(reagentDispenser.PackPrototypeId, out ReagentDispenserInventoryPrototype? packPrototype))
            {
                inventory.AddRange(packPrototype.Inventory.Select(x => new ReagentId(x, null)));
            }

            if (HasComp<EmaggedComponent>(reagentDispenser.Owner)
                && reagentDispenser.EmagPackPrototypeId is not null
                && _prototypeManager.TryIndex(reagentDispenser.EmagPackPrototypeId, out ReagentDispenserInventoryPrototype? emagPackPrototype))
            {
                inventory.AddRange(emagPackPrototype.Inventory.Select(x => new ReagentId(x, null)));
            }

            return inventory;
        }

        private void OnEmagged(EntityUid uid, ReagentDispenserComponent reagentDispenser, ref GotEmaggedEvent args)
        {
            // adding component manually to have correct state
            EntityManager.AddComponent<EmaggedComponent>(uid);
            UpdateUiState(reagentDispenser);
            args.Handled = true;
        }

        private void OnSetDispenseAmountMessage(EntityUid uid, ReagentDispenserComponent reagentDispenser, ReagentDispenserSetDispenseAmountMessage message)
        {
            reagentDispenser.DispenseAmount = message.ReagentDispenserDispenseAmount;
            UpdateUiState(reagentDispenser);
            ClickSound(reagentDispenser);
        }

        private void OnDispenseReagentMessage(EntityUid uid, ReagentDispenserComponent reagentDispenser, ReagentDispenserDispenseReagentMessage message)
        {
            // Ensure that the reagent is something this reagent dispenser can dispense.
            if (!GetInventory(reagentDispenser).Contains(message.ReagentId))
                return;

            var outputContainer = _itemSlotsSystem.GetItemOrNull(reagentDispenser.Owner, SharedReagentDispenser.OutputSlotName);
            if (outputContainer is not {Valid: true} || !_solutionContainerSystem.TryGetFitsInDispenser(outputContainer.Value, out var solution))
                return;

            if (_solutionContainerSystem.TryAddReagent(outputContainer.Value, solution, message.ReagentId, (int)reagentDispenser.DispenseAmount, out var dispensedAmount)
                && message.Session.AttachedEntity is not null)
            {
                _adminLogger.Add(LogType.ChemicalReaction, LogImpact.Medium,
                    $"{ToPrettyString(message.Session.AttachedEntity.Value):player} dispensed {dispensedAmount}u of {message.ReagentId} into {ToPrettyString(outputContainer.Value):entity}");
            }

            UpdateUiState(reagentDispenser);
            ClickSound(reagentDispenser);
        }

        private void OnClearContainerSolutionMessage(EntityUid uid, ReagentDispenserComponent reagentDispenser, ReagentDispenserClearContainerSolutionMessage message)
        {
            var outputContainer = _itemSlotsSystem.GetItemOrNull(reagentDispenser.Owner, SharedReagentDispenser.OutputSlotName);
            if (outputContainer is not {Valid: true} || !_solutionContainerSystem.TryGetFitsInDispenser(outputContainer.Value, out var solution))
                return;

            _solutionContainerSystem.RemoveAllSolution(outputContainer.Value, solution);
            UpdateUiState(reagentDispenser);
            ClickSound(reagentDispenser);
        }

        private void ClickSound(ReagentDispenserComponent reagentDispenser)
        {
            _audioSystem.PlayPvs(reagentDispenser.ClickSound, reagentDispenser.Owner, AudioParams.Default.WithVolume(-2f));
        }
    }
}
