using System.Linq;
using Content.Shared.Chemistry.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Labels.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using JetBrains.Annotations;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chemistry.EntitySystems;

/// <summary>
/// Contains all the server-side logic for reagent dispensers.
/// <seealso cref="ReagentDispenserComponent"/>
/// </summary>
[UsedImplicitly]
public sealed class ReagentDispenserSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly SolutionTransferSystem _solutionTransferSystem = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly OpenableSystem _openable = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReagentDispenserComponent, ComponentStartup>(SubscribeUpdateUiState);
        SubscribeLocalEvent<ReagentDispenserComponent, SolutionContainerChangedEvent>(SubscribeUpdateUiState);
        SubscribeLocalEvent<ReagentDispenserComponent, EntInsertedIntoContainerMessage>(SubscribeUpdateUiState, after: [typeof(SharedStorageSystem)]);
        SubscribeLocalEvent<ReagentDispenserComponent, EntRemovedFromContainerMessage>(SubscribeUpdateUiState, after: [typeof(SharedStorageSystem)]);
        SubscribeLocalEvent<ReagentDispenserComponent, BoundUIOpenedEvent>(SubscribeUpdateUiState);

        Subs.BuiEvents<ReagentDispenserComponent>(ReagentDispenserUiKey.Key,
            subs =>
            {
                subs.Event<ReagentDispenserSetDispenseAmountMessage>(OnSetDispenseAmountMessage);
                subs.Event<ReagentDispenserDispenseReagentMessage>(OnDispenseReagentMessage);
                subs.Event<ReagentDispenserEjectContainerMessage>(OnEjectReagentMessage);
                subs.Event<ReagentDispenserClearContainerSolutionMessage>(OnClearContainerSolutionMessage);
            });

        SubscribeLocalEvent<ReagentDispenserComponent, MapInitEvent>(OnMapInit, before: [typeof(ItemSlotsSystem)]);
    }

    private void SubscribeUpdateUiState<T>(Entity<ReagentDispenserComponent> ent, ref T ev)
    {
        UpdateUiState(ent);
    }

    private void UpdateUiState(Entity<ReagentDispenserComponent> reagentDispenser)
    {
        var outputContainer = _itemSlotsSystem.GetItemOrNull(reagentDispenser, ReagentDispenserComponent.OutputSlotName);
        var outputContainerInfo = BuildOutputContainerInfo(outputContainer);

        var inventory = GetInventory(reagentDispenser);

        var state = new ReagentDispenserBoundUserInterfaceState(outputContainerInfo,
            GetNetEntity(outputContainer),
            inventory,
            reagentDispenser.Comp.SelectableAmounts,
            reagentDispenser.Comp.DispenseAmount);
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
            // For some reason on the edge of PVS parts of the storage will not exist
            // I don't really get it but this solves the problem and once close to the
            // dispenser it's fine. If it's invalid on server... we have worse problems.
            if (!storedContainer.Valid)
                continue;

            string reagentLabel;
            if (TryComp<LabelComponent>(storedContainer, out var label) && !string.IsNullOrEmpty(label.CurrentLabel))
                reagentLabel = label.CurrentLabel;
            else
                reagentLabel = Name(storedContainer);

            // Get volume remaining and color of solution
            var quantity = FixedPoint2.Zero;
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
        // No QOL-improving hacked clients allowed! Or something.
        if (!reagentDispenser.Comp.SelectableAmounts.Contains(message.ReagentDispenserDispenseAmount))
            return;

        reagentDispenser.Comp.DispenseAmount = message.ReagentDispenserDispenseAmount;
        Dirty(reagentDispenser);
        UpdateUiState(reagentDispenser);
        ClickSound(reagentDispenser, message.Actor);
    }

    private void OnDispenseReagentMessage(Entity<ReagentDispenserComponent> reagentDispenser, ref ReagentDispenserDispenseReagentMessage message)
    {
        if (!TryComp<StorageComponent>(reagentDispenser.Owner, out var storage))
            return;

        // Ensure that the reagent is something this reagent dispenser can dispense.
        var storageLocation = message.StorageLocation;
        var storedContainer = storage.StoredItems.FirstOrDefault(kvp => kvp.Value == storageLocation).Key;
        if (storedContainer == EntityUid.Invalid)
            return;

        var outputContainer = _itemSlotsSystem.GetItemOrNull(reagentDispenser, ReagentDispenserComponent.OutputSlotName);
        if (outputContainer is not { Valid: true } || !HasComp<FitsInDispenserComponent>(outputContainer))
            return;

        if (_solutionContainerSystem.TryGetDrainableSolution(storedContainer, out var src, out _)
            && _solutionContainerSystem.TryGetRefillableSolution(outputContainer.Value, out var dst, out _))
        {
            // force open container, if applicable, to avoid confusing people on why it doesn't dispense
            _openable.SetOpen(storedContainer);
            _solutionTransferSystem.Transfer(reagentDispenser,
                storedContainer,
                src.Value,
                outputContainer.Value,
                dst.Value,
                reagentDispenser.Comp.DispenseAmount);
        }

        UpdateUiState(reagentDispenser);
        ClickSound(reagentDispenser, message.Actor);
    }

    private void OnEjectReagentMessage(Entity<ReagentDispenserComponent> reagentDispenser, ref ReagentDispenserEjectContainerMessage message)
    {
        if (!TryComp<StorageComponent>(reagentDispenser.Owner, out var storage))
            return;

        var storageLocation = message.StorageLocation;
        var storedContainer = storage.StoredItems.FirstOrDefault(kvp => kvp.Value == storageLocation).Key;
        if (storedContainer == EntityUid.Invalid)
            return;

        _handsSystem.TryPickupAnyHand(message.Actor, storedContainer);
    }

    private void OnClearContainerSolutionMessage(Entity<ReagentDispenserComponent> reagentDispenser, ref ReagentDispenserClearContainerSolutionMessage message)
    {
        var outputContainer =
            _itemSlotsSystem.GetItemOrNull(reagentDispenser, ReagentDispenserComponent.OutputSlotName);
        if (outputContainer is not { Valid: true }
            || !_solutionContainerSystem.TryGetFitsInDispenser(outputContainer.Value, out var solution, out _))
            return;

        _solutionContainerSystem.RemoveAllSolution(solution.Value);
        UpdateUiState(reagentDispenser);
        ClickSound(reagentDispenser, message.Actor);
    }

    private void ClickSound(Entity<ReagentDispenserComponent> reagentDispenser, EntityUid actor)
    {
        _audioSystem.PlayPredicted(reagentDispenser.Comp.ClickSound, reagentDispenser, actor);
    }

    private void OnMapInit(Entity<ReagentDispenserComponent> ent, ref MapInitEvent args)
    {
        _itemSlotsSystem.AddItemSlot(ent.Owner, ReagentDispenserComponent.OutputSlotName, ent.Comp.BeakerSlot);
    }
}
