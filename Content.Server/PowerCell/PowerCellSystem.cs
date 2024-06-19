using Content.Server.Emp;
using Content.Server.Power.Components;
using Content.Shared.Examine;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared.Rounding;
using Robust.Shared.Containers;
using System.Diagnostics.CodeAnalysis;
using Content.Server.Kitchen.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.UserInterface;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Popups;
using ActivatableUISystem = Content.Shared.UserInterface.ActivatableUISystem;

namespace Content.Server.PowerCell;

/// <summary>
/// Handles Power cells
/// </summary>
public sealed partial class PowerCellSystem : SharedPowerCellSystem
{
    [Dependency] private readonly ActivatableUISystem _activatable = default!;
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _sharedAppearanceSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly RiggableSystem _riggableSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PowerCellComponent, ChargeChangedEvent>(OnChargeChanged);
        SubscribeLocalEvent<PowerCellComponent, ExaminedEvent>(OnCellExamined);
        SubscribeLocalEvent<PowerCellComponent, EmpAttemptEvent>(OnCellEmpAttempt);

        SubscribeLocalEvent<PowerCellDrawComponent, ChargeChangedEvent>(OnDrawChargeChanged);
        SubscribeLocalEvent<PowerCellDrawComponent, PowerCellChangedEvent>(OnDrawCellChanged);

        // funny
        SubscribeLocalEvent<PowerCellSlotComponent, ExaminedEvent>(OnCellSlotExamined);
        SubscribeLocalEvent<PowerCellSlotComponent, BeingMicrowavedEvent>(OnSlotMicrowaved);
    }

    private void OnSlotMicrowaved(EntityUid uid, PowerCellSlotComponent component, BeingMicrowavedEvent args)
    {
        if (!_itemSlotsSystem.TryGetSlot(uid, component.CellSlotId, out var slot))
            return;

        if (slot.Item == null)
            return;

        RaiseLocalEvent(slot.Item.Value, args);
    }

    private void OnChargeChanged(EntityUid uid, PowerCellComponent component, ref ChargeChangedEvent args)
    {
        if (TryComp<RiggableComponent>(uid, out var rig) && rig.IsRigged)
        {
            _riggableSystem.Explode(uid, cause: null);
            return;
        }

        var frac = args.Charge / args.MaxCharge;
        var level = (byte) ContentHelpers.RoundToNearestLevels(frac, 1, PowerCellComponent.PowerCellVisualsLevels);
        _sharedAppearanceSystem.SetData(uid, PowerCellVisuals.ChargeLevel, level);

        // If this power cell is inside a cell-slot, inform that entity that the power has changed (for updating visuals n such).
        if (_containerSystem.TryGetContainingContainer(uid, out var container)
            && TryComp(container.Owner, out PowerCellSlotComponent? slot)
            && _itemSlotsSystem.TryGetSlot(container.Owner, slot.CellSlotId, out var itemSlot))
        {
            if (itemSlot.Item == uid)
                RaiseLocalEvent(container.Owner, new PowerCellChangedEvent(false));
        }
    }

    protected override void OnCellRemoved(EntityUid uid, PowerCellSlotComponent component, EntRemovedFromContainerMessage args)
    {
        base.OnCellRemoved(uid, component, args);

        if (args.Container.ID != component.CellSlotId)
            return;

        var ev = new PowerCellSlotEmptyEvent();
        RaiseLocalEvent(uid, ref ev);
    }

    #region Activatable
    /// <inheritdoc/>
    public override bool HasActivatableCharge(EntityUid uid, PowerCellDrawComponent? battery = null, PowerCellSlotComponent? cell = null, EntityUid? user = null)
    {
        // Default to true if we don't have the components.
        if (!Resolve(uid, ref battery, ref cell, false))
            return true;

        return HasCharge(uid, battery.UseRate, cell, user);
    }

    /// <summary>
    /// Tries to use the <see cref="PowerCellDrawComponent.UseRate"/> for this entity.
    /// </summary>
    /// <param name="user">Popup to this user with the relevant detail if specified.</param>
    public bool TryUseActivatableCharge(EntityUid uid, PowerCellDrawComponent? battery = null, PowerCellSlotComponent? cell = null, EntityUid? user = null)
    {
        // Default to true if we don't have the components.
        if (!Resolve(uid, ref battery, ref cell, false))
            return true;

        if (TryUseCharge(uid, battery.UseRate, cell, user))
        {
            _sharedAppearanceSystem.SetData(uid, PowerCellSlotVisuals.Enabled, HasActivatableCharge(uid, battery, cell, user));
            _activatable.CheckUsage(uid);
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public override bool HasDrawCharge(
        EntityUid uid,
        PowerCellDrawComponent? battery = null,
        PowerCellSlotComponent? cell = null,
        EntityUid? user = null)
    {
        if (!Resolve(uid, ref battery, ref cell, false))
            return true;

        return HasCharge(uid, battery.DrawRate, cell, user);
    }

    #endregion

    /// <summary>
    /// Returns whether the entity has a slotted battery and charge for the requested action.
    /// </summary>
    /// <param name="user">Popup to this user with the relevant detail if specified.</param>
    public bool HasCharge(EntityUid uid, float charge, PowerCellSlotComponent? component = null, EntityUid? user = null)
    {
        if (!TryGetBatteryFromSlot(uid, out var battery, component))
        {
            if (user != null)
                _popup.PopupEntity(Loc.GetString("power-cell-no-battery"), uid, user.Value);

            return false;
        }

        if (battery.CurrentCharge < charge)
        {
            if (user != null)
                _popup.PopupEntity(Loc.GetString("power-cell-insufficient"), uid, user.Value);

            return false;
        }

        return true;
    }

    /// <summary>
    /// Tries to use charge from a slotted battery.
    /// </summary>
    public bool TryUseCharge(EntityUid uid, float charge, PowerCellSlotComponent? component = null, EntityUid? user = null)
    {
        if (!TryGetBatteryFromSlot(uid, out var batteryEnt, out var battery, component))
        {
            if (user != null)
                _popup.PopupEntity(Loc.GetString("power-cell-no-battery"), uid, user.Value);

            return false;
        }

        if (!_battery.TryUseCharge(batteryEnt.Value, charge, battery))
        {
            if (user != null)
                _popup.PopupEntity(Loc.GetString("power-cell-insufficient"), uid, user.Value);

            return false;
        }

        _sharedAppearanceSystem.SetData(uid, PowerCellSlotVisuals.Enabled, battery.CurrentCharge > 0);
        return true;
    }

    public bool TryGetBatteryFromSlot(EntityUid uid, [NotNullWhen(true)] out BatteryComponent? battery, PowerCellSlotComponent? component = null)
    {
        return TryGetBatteryFromSlot(uid, out _, out battery, component);
    }

    public bool TryGetBatteryFromSlot(EntityUid uid,
        [NotNullWhen(true)] out EntityUid? batteryEnt,
        [NotNullWhen(true)] out BatteryComponent? battery,
        PowerCellSlotComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
        {
            batteryEnt = null;
            battery = null;
            return false;
        }

        if (_itemSlotsSystem.TryGetSlot(uid, component.CellSlotId, out ItemSlot? slot))
        {
            batteryEnt = slot.Item;
            return TryComp(slot.Item, out battery);
        }

        batteryEnt = null;
        battery = null;
        return false;
    }

    private void OnCellExamined(EntityUid uid, PowerCellComponent component, ExaminedEvent args)
    {
        TryComp<BatteryComponent>(uid, out var battery);
        OnBatteryExamined(uid, battery, args);
    }

    private void OnCellEmpAttempt(EntityUid uid, PowerCellComponent component, EmpAttemptEvent args)
    {
        var parent = Transform(uid).ParentUid;
        // relay the attempt event to the slot so it can cancel it
        if (HasComp<PowerCellSlotComponent>(parent))
            RaiseLocalEvent(parent, args);
    }

    private void OnCellSlotExamined(EntityUid uid, PowerCellSlotComponent component, ExaminedEvent args)
    {
        TryGetBatteryFromSlot(uid, out var battery);
        OnBatteryExamined(uid, battery, args);
    }

    private void OnBatteryExamined(EntityUid uid, BatteryComponent? component, ExaminedEvent args)
    {
        if (component != null)
        {
            var charge = component.CurrentCharge / component.MaxCharge * 100;
            args.PushMarkup(Loc.GetString("power-cell-component-examine-details", ("currentCharge", $"{charge:F0}")));
        }
        else
        {
            args.PushMarkup(Loc.GetString("power-cell-component-examine-details-no-battery"));
        }
    }
}
