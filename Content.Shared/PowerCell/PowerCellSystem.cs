using Content.Shared.Containers.ItemSlots;
using Content.Shared.PowerCell.Components;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.PowerCell;

public sealed partial class PowerCellSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly PredictedBatterySystem _battery = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        InitializeRelay();

        SubscribeLocalEvent<PowerCellSlotComponent, ContainerIsInsertingAttemptEvent>(OnCellSlotInsertAttempt);
        SubscribeLocalEvent<PowerCellSlotComponent, EntInsertedIntoContainerMessage>(OnCellSlotInserted);
        SubscribeLocalEvent<PowerCellSlotComponent, EntRemovedFromContainerMessage>(OnCellSlotRemoved);
        SubscribeLocalEvent<PowerCellSlotComponent, ExaminedEvent>(OnCellSlotExamined);
        SubscribeLocalEvent<PowerCellSlotComponent, PredictedBatteryStateChangedEvent>(OnCellSlotStateChanged);

        SubscribeLocalEvent<PowerCellComponent, ExaminedEvent>(OnCellExamined);

        SubscribeLocalEvent<PowerCellDrawComponent, RefreshChargeRateEvent>(OnDrawRefreshChargeRate);
        SubscribeLocalEvent<PowerCellDrawComponent, ComponentStartup>(OnDrawStartup);
        SubscribeLocalEvent<PowerCellDrawComponent, ComponentRemove>(OnDrawRemove);

    }

    private void OnCellSlotInsertAttempt(Entity<PowerCellSlotComponent> ent, ref ContainerIsInsertingAttemptEvent args)
    {
        if (!ent.Comp.Initialized)
            return;

        if (args.Container.ID != ent.Comp.CellSlotId)
            return;

        // TODO: Can't this just use the ItemSlot's whitelist?
        if (!HasComp<PowerCellComponent>(args.EntityUid))
            args.Cancel();
    }

    private void OnCellSlotInserted(Entity<PowerCellSlotComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.CellSlotId)
            return;

        if (_timing.ApplyingState)
            return; // The change in appearance data is already networked separately.


        var ev = new PowerCellChangedEvent(false);
        RaiseLocalEvent(ent, ref ev);

        _battery.RefreshChargeRate(args.Entity);

        // Only update the visuals if we actually use them.
        if (!HasComp<PredictedBatteryVisualsComponent>(ent))
            return;

        // Set the data to that of the power cell
        if (_appearance.TryGetData(args.Entity, BatteryVisuals.State, out BatteryState state))
            _appearance.SetData(ent.Owner, BatteryVisuals.State, state);

        // Set the data to that of the power cell
        if (_appearance.TryGetData(args.Entity, BatteryVisuals.Charging, out BatteryChargingState charging))
            _appearance.SetData(ent.Owner, BatteryVisuals.Charging, charging);
    }

    private void OnCellSlotRemoved(Entity<PowerCellSlotComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.CellSlotId)
            return;

        if (_timing.ApplyingState)
            return; // The change in appearance data is already networked separately.

        var ev = new PowerCellChangedEvent(true);
        RaiseLocalEvent(ent, ref ev);

        var emptyEv = new PowerCellSlotEmptyEvent();
        RaiseLocalEvent(ent, ref emptyEv);

        _battery.RefreshChargeRate(args.Entity);

        // Only update the visuals if we actually use them.
        if (!HasComp<PredictedBatteryVisualsComponent>(ent))
            return;

        // Set the appearance to empty.
        _appearance.SetData(ent.Owner, BatteryVisuals.State, BatteryState.Empty);
        _appearance.SetData(ent.Owner, BatteryVisuals.Charging, BatteryChargingState.Constant);
    }


    private void OnCellSlotStateChanged(Entity<PowerCellSlotComponent> ent, ref PredictedBatteryStateChangedEvent args)
    {
        if (args.NewState != BatteryState.Empty)
            return;

        // Inform the device that the battery is empty.
        var ev = new PowerCellSlotEmptyEvent();
        RaiseLocalEvent(ent, ref ev);
    }

    private void OnCellSlotExamined(Entity<PowerCellSlotComponent> ent, ref ExaminedEvent args)
    {
        if (TryGetBatteryFromSlot(ent.AsNullable(), out var battery))
            OnBatteryExamined(battery.Value, ref args);
        else
            args.PushMarkup(Loc.GetString("power-cell-component-examine-details-no-battery"));
    }

    private void OnCellExamined(Entity<PowerCellComponent> ent, ref ExaminedEvent args)
    {
        if (TryComp<PredictedBatteryComponent>(ent, out var battery))
            OnBatteryExamined((ent.Owner, battery), ref args);
    }

    private void OnBatteryExamined(Entity<PredictedBatteryComponent> ent, ref ExaminedEvent args)
    {
        var chargePercent = _battery.GetChargeLevel(ent.AsNullable()) * 100;
        args.PushMarkup(Loc.GetString("power-cell-component-examine-details", ("currentCharge", $"{chargePercent:F0}")));
    }

    private void OnDrawRefreshChargeRate(Entity<PowerCellDrawComponent> ent, ref RefreshChargeRateEvent args)
    {
        if (ent.Comp.Enabled)
            args.NewChargeRate -= ent.Comp.DrawRate;
    }

    private void OnDrawStartup(Entity<PowerCellDrawComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.Enabled)
            _battery.RefreshChargeRate(ent.Owner);
    }

    private void OnDrawRemove(Entity<PowerCellDrawComponent> ent, ref ComponentRemove args)
    {
        // We use ComponentRemove to make sure this component no longer subscribes to the refresh event.
        if (ent.Comp.Enabled)
            _battery.RefreshChargeRate(ent.Owner);
    }
}
