using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Shared.PowerCell.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using System;

namespace Content.Shared.PowerCell;

public abstract class SharedPowerCellSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PowerCellSlotComponent, ComponentInit>(OnCellSlotInit);
        SubscribeLocalEvent<PowerCellSlotComponent, ComponentRemove>(OnCellSlotRemove);

        SubscribeLocalEvent<PowerCellSlotComponent, ExaminedEvent>(OnSlotExamined);

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

        if (!TryComp(args.EntityUid, out PowerCellComponent? cell) || cell.CellSize != component.SlotSize)
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
        _itemSlotsSystem.AddItemSlot(uid, "cellslot_cell_container", component.CellSlot);

        if (string.IsNullOrWhiteSpace(component.CellSlot.Name) &&
            !string.IsNullOrWhiteSpace(component.SlotName))
        {
            component.CellSlot.Name = component.SlotName;
        }

        if (component.StartEmpty)
            return;

        if (!string.IsNullOrWhiteSpace(component.CellSlot.StartingItem))
            return;

        // set default starting cell based on cell-type
        component.CellSlot.StartingItem = component.SlotSize switch
        {
            PowerCellSize.Small => "PowerCellSmallStandard",
            PowerCellSize.Medium => "PowerCellMediumStandard",
            PowerCellSize.Large => "PowerCellLargeStandard",
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private void OnCellSlotRemove(EntityUid uid, PowerCellSlotComponent component, ComponentRemove args)
    {
        _itemSlotsSystem.RemoveItemSlot(uid, component.CellSlot);
    }

    private void OnSlotExamined(EntityUid uid, PowerCellSlotComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || string.IsNullOrWhiteSpace(component.DescFormatString))
            return;

        var sizeText = Loc.GetString(component.SlotSize switch
        {
            PowerCellSize.Small => "power-cell-slot-component-description-size-small",
            PowerCellSize.Medium => "power-cell-slot-component-description-size-medium",
            PowerCellSize.Large => "power-cell-slot-component-description-size-large",
            _ => "???"
        });

        args.PushMarkup(Loc.GetString(component.DescFormatString, ("size", sizeText)));
    }
}
