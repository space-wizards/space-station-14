using Content.Shared.Containers.ItemSlots;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.PowerCell.Components;
using Content.Shared.Rejuvenate;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.PowerCell;

public abstract class SharedPowerCellSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] protected readonly ItemToggleSystem Toggle = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PowerCellSlotComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<PowerCellSlotComponent, EntInsertedIntoContainerMessage>(OnCellInserted);
        SubscribeLocalEvent<PowerCellSlotComponent, EntRemovedFromContainerMessage>(OnCellRemoved);
        SubscribeLocalEvent<PowerCellSlotComponent, ContainerIsInsertingAttemptEvent>(OnCellInsertAttempt);

        SubscribeLocalEvent<PowerCellDrawComponent, ItemToggleActivateAttemptEvent>(OnActivateAttempt);
        SubscribeLocalEvent<PowerCellDrawComponent, ItemToggledEvent>(OnToggled);
    }

    private void OnRejuvenate(EntityUid uid, PowerCellSlotComponent component, RejuvenateEvent args)
    {
        if (!_itemSlots.TryGetSlot(uid, component.CellSlotId, out var itemSlot) || !itemSlot.Item.HasValue)
            return;

        // charge entity batteries and remove booby traps.
        RaiseLocalEvent(itemSlot.Item.Value, args);
    }

    private void OnCellInsertAttempt(EntityUid uid, PowerCellSlotComponent component, ContainerIsInsertingAttemptEvent args)
    {
        if (!component.Initialized)
            return;

        if (args.Container.ID != component.CellSlotId)
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

        if (args.Container.ID != component.CellSlotId)
            return;
        _appearance.SetData(uid, PowerCellSlotVisuals.Enabled, true);
        RaiseLocalEvent(uid, new PowerCellChangedEvent(false), false);
    }

    protected virtual void OnCellRemoved(EntityUid uid, PowerCellSlotComponent component, EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != component.CellSlotId)
            return;
        _appearance.SetData(uid, PowerCellSlotVisuals.Enabled, false);
        RaiseLocalEvent(uid, new PowerCellChangedEvent(true), false);
    }

    private void OnActivateAttempt(Entity<PowerCellDrawComponent> ent, ref ItemToggleActivateAttemptEvent args)
    {
        if (!HasDrawCharge(ent, ent.Comp, user: args.User)
            || !HasActivatableCharge(ent, ent.Comp, user: args.User))
            args.Cancelled = true;
    }

    private void OnToggled(Entity<PowerCellDrawComponent> ent, ref ItemToggledEvent args)
    {
        ent.Comp.NextUpdateTime = Timing.CurTime;
    }

    public void SetDrawEnabled(Entity<PowerCellDrawComponent?> ent, bool enabled)
    {
        if (!Resolve(ent, ref ent.Comp, false) || ent.Comp.Enabled == enabled)
            return;

        ent.Comp.Enabled = enabled;
        Dirty(ent, ent.Comp);
    }

    /// <summary>
    /// Returns whether the entity has a slotted battery and <see cref="PowerCellDrawComponent.UseRate"/> charge.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="battery"></param>
    /// <param name="cell"></param>
    /// <param name="user">Popup to this user with the relevant detail if specified.</param>
    public abstract bool HasActivatableCharge(
        EntityUid uid,
        PowerCellDrawComponent? battery = null,
        PowerCellSlotComponent? cell = null,
        EntityUid? user = null);

    /// <summary>
    /// Whether the power cell has any power at all for the draw rate.
    /// </summary>
    public abstract bool HasDrawCharge(
        EntityUid uid,
        PowerCellDrawComponent? battery = null,
        PowerCellSlotComponent? cell = null,
        EntityUid? user = null);
}
