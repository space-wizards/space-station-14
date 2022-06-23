using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Shared.PowerCell.Components;
using Robust.Shared.Containers;

namespace Content.Shared.PowerCell;

public abstract class SharedPowerCellSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;

    public const string CellSlotContainer = "cell_slot";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PowerCellSlotComponent, ComponentInit>(OnCellSlotInit);
        SubscribeLocalEvent<PowerCellSlotComponent, ComponentRemove>(OnCellSlotRemove);

        SubscribeLocalEvent<PowerCellSlotComponent, EntInsertedIntoContainerMessage>(OnCellInserted);
        SubscribeLocalEvent<PowerCellSlotComponent, EntRemovedFromContainerMessage>(OnCellRemoved);
        SubscribeLocalEvent<PowerCellSlotComponent, ContainerIsInsertingAttemptEvent>(OnCellInsertAttempt);
    }

    private void OnCellInsertAttempt(EntityUid uid, PowerCellSlotComponent component, ContainerIsInsertingAttemptEvent args)
    {
        if (!component.Initialized)
            return;

        if (args.Container.ID != component.CellSlot.ID)
            return;

        if (!HasComp<PowerCellComponent>(args.EntityUid))
        {
            args.Cancel();
        }
    }

    private void OnCellInserted(EntityUid uid, PowerCellSlotComponent component, EntInsertedIntoContainerMessage args)
    {
        if (!component.Initialized)
            return;

        if (args.Container.ID != component.CellSlot.ID)
            return;

        RaiseLocalEvent(uid, new PowerCellChangedEvent(false), false);
    }

    private void OnCellRemoved(EntityUid uid, PowerCellSlotComponent component, EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != component.CellSlot.ID)
            return;

        RaiseLocalEvent(uid, new PowerCellChangedEvent(true), false);
    }

    private void OnCellSlotInit(EntityUid uid, PowerCellSlotComponent component, ComponentInit args)
    {
        _itemSlotsSystem.AddItemSlot(uid, CellSlotContainer, component.CellSlot);

        if (string.IsNullOrWhiteSpace(component.CellSlot.Name) &&
            !string.IsNullOrWhiteSpace(component.SlotName))
        {
            component.CellSlot.Name = component.SlotName;
        }
    }

    private void OnCellSlotRemove(EntityUid uid, PowerCellSlotComponent component, ComponentRemove args)
    {
        _itemSlotsSystem.RemoveItemSlot(uid, component.CellSlot);
    }
}
