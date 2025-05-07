using Content.Shared.Containers.ItemSlots;
using Content.Shared.IdentityManagement;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.PowerCell.Components;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.UserInterface;
using Content.Shared.Wires;
using Robust.Shared.Containers;

namespace Content.Shared.Silicons.Borgs;

/// <summary>
/// This handles logic, interactions, and UI related to <see cref="BorgChassisComponent"/> and other related components.
/// </summary>
public abstract partial class SharedBorgSystem : EntitySystem
{
    [Dependency] protected readonly SharedContainerSystem Container = default!;
    [Dependency] protected readonly ItemSlotsSystem ItemSlots = default!;
    [Dependency] protected readonly ItemToggleSystem Toggle = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorgChassisComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<BorgChassisComponent, ItemSlotInsertAttemptEvent>(OnItemSlotInsertAttempt);
        SubscribeLocalEvent<BorgChassisComponent, ItemSlotEjectAttemptEvent>(OnItemSlotEjectAttempt);
        SubscribeLocalEvent<BorgChassisComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<BorgChassisComponent, EntRemovedFromContainerMessage>(OnRemoved);
        SubscribeLocalEvent<BorgChassisComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeedModifiers);
        SubscribeLocalEvent<BorgChassisComponent, ActivatableUIOpenAttemptEvent>(OnUIOpenAttempt);
        SubscribeLocalEvent<TryGetIdentityShortInfoEvent>(OnTryGetIdentityShortInfo);

        InitializeRelay();
    }

    private void OnTryGetIdentityShortInfo(TryGetIdentityShortInfoEvent args)
    {
        if (args.Handled)
        {
            return;
        }

        if (!HasComp<BorgChassisComponent>(args.ForActor))
        {
            return;
        }

        args.Title = Name(args.ForActor).Trim();
        args.Handled = true;
    }

    private void OnItemSlotInsertAttempt(EntityUid uid, BorgChassisComponent component, ref ItemSlotInsertAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TryComp<PowerCellSlotComponent>(uid, out var cellSlotComp) ||
            !TryComp<WiresPanelComponent>(uid, out var panel))
            return;

        if (!ItemSlots.TryGetSlot(uid, cellSlotComp.CellSlotId, out var cellSlot) || cellSlot != args.Slot)
            return;

        if (!panel.Open || args.User == uid)
            args.Cancelled = true;
    }

    private void OnItemSlotEjectAttempt(EntityUid uid, BorgChassisComponent component, ref ItemSlotEjectAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TryComp<PowerCellSlotComponent>(uid, out var cellSlotComp) ||
            !TryComp<WiresPanelComponent>(uid, out var panel))
            return;

        if (!ItemSlots.TryGetSlot(uid, cellSlotComp.CellSlotId, out var cellSlot) || cellSlot != args.Slot)
            return;

        if (!panel.Open || args.User == uid)
            args.Cancelled = true;
    }

    private void OnStartup(EntityUid uid, BorgChassisComponent component, ComponentStartup args)
    {
        if (!TryComp<ContainerManagerComponent>(uid, out var containerManager))
            return;

        component.BrainContainer = Container.EnsureContainer<ContainerSlot>(uid, component.BrainContainerId, containerManager);
        component.ModuleContainer = Container.EnsureContainer<Container>(uid, component.ModuleContainerId, containerManager);
    }

    private void OnUIOpenAttempt(EntityUid uid, BorgChassisComponent component, ActivatableUIOpenAttemptEvent args)
    {
        // borgs can't view their own ui
        if (args.User == uid)
            args.Cancel();
    }

    protected virtual void OnInserted(EntityUid uid, BorgChassisComponent component, EntInsertedIntoContainerMessage args)
    {

    }

    protected virtual void OnRemoved(EntityUid uid, BorgChassisComponent component, EntRemovedFromContainerMessage args)
    {

    }

    private void OnRefreshMovementSpeedModifiers(EntityUid uid, BorgChassisComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (Toggle.IsActivated(uid))
            return;

        if (!TryComp<MovementSpeedModifierComponent>(uid, out var movement))
            return;

        var sprintDif = movement.BaseWalkSpeed / movement.BaseSprintSpeed;
        args.ModifySpeed(1f, sprintDif);
    }

    /// <summary>
    /// Sets <see cref="BorgModuleComponent.DefaultModule"/>.
    /// </summary>
    public void SetBorgModuleDefault(Entity<BorgModuleComponent> ent, bool newDefault)
    {
        ent.Comp.DefaultModule = newDefault;
        Dirty(ent);
    }
}
