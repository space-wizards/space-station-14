using Content.Server.Power.Components;
using Content.Server.PowerCell;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Shared.PowerCell.Components;
using JetBrains.Annotations;
using Robust.Shared.Containers;

namespace Content.Server.Power.EntitySystems;

[UsedImplicitly]
internal sealed class ChargerSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly PowerCellSystem _cellSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ChargerComponent, ComponentInit>(OnChargerInit);
        SubscribeLocalEvent<ChargerComponent, ComponentRemove>(OnChargerRemove);
        SubscribeLocalEvent<ChargerComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<ChargerComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<ChargerComponent, EntRemovedFromContainerMessage>(OnRemoved);
        SubscribeLocalEvent<ChargerComponent, ContainerIsInsertingAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<ChargerComponent, ExaminedEvent>(OnChargerExamine);
    }

    private void OnChargerExamine(EntityUid uid, ChargerComponent component, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("charger-examine", ("color", "yellow"), ("chargeRate", component.ChargeRate)));
    }

    public override void Update(float frameTime)
    {
        foreach (var comp in EntityManager.EntityQuery<ChargerComponent>())
        {
            comp.OnUpdate(frameTime);
        }
    }
    private void OnChargerInit(EntityUid uid, ChargerComponent component, ComponentInit args)
    {
        _itemSlotsSystem.AddItemSlot(uid, "charger-slot", component.ChargerSlot);
    }

    private void OnChargerRemove(EntityUid uid, ChargerComponent component, ComponentRemove args)
    {
        _itemSlotsSystem.RemoveItemSlot(uid, component.ChargerSlot);
    }

    private void OnPowerChanged(EntityUid uid, ChargerComponent component, ref PowerChangedEvent args)
    {
        component.UpdateStatus();
    }

    private void OnInserted(EntityUid uid, ChargerComponent component, EntInsertedIntoContainerMessage args)
    {
        if (!component.Initialized)
            return;

        if (args.Container.ID != component.ChargerSlot.ID)
            return;

        // try get a battery directly on the inserted entity
        if (!TryComp(args.Entity, out component.HeldBattery))
        {
            // or by checking for a power cell slot on the inserted entity
            _cellSystem.TryGetBatteryFromSlot(args.Entity, out component.HeldBattery);
        }

        component.UpdateStatus();
    }

    private void OnRemoved(EntityUid uid, ChargerComponent component, EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != component.ChargerSlot.ID)
            return;

        component.UpdateStatus();
    }

    /// <summary>
    ///     Verify that the entity being inserted is actually rechargeable.
    /// </summary>
    private void OnInsertAttempt(EntityUid uid, ChargerComponent component, ContainerIsInsertingAttemptEvent args)
    {
        if (!component.Initialized)
            return;

        if (args.Container.ID != component.ChargerSlot.ID)
            return;

        if (!TryComp(args.EntityUid, out PowerCellSlotComponent? cellSlot))
            return;

        if (!_itemSlotsSystem.TryGetSlotById(args.EntityUid, cellSlot.CellSlotId, out ItemSlot? itemSlot))
            return;

        if (!cellSlot.FitsInCharger || !itemSlot.HasItem)
            args.Cancel();
    }
}
