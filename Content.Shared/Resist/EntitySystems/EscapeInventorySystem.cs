using Content.Shared.ActionBlocker;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Content.Shared.Resist.Components;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Robust.Shared.Containers;

namespace Content.Shared.Resist.EntitySystems;

/// <summary>
/// Handles allowing entities with <see cref="CanEscapeInventoryComponent"/> to escape from containers or inventories by moving.
/// </summary>
public sealed partial class EscapeInventorySystem : EntitySystem
{
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private SharedHandsSystem _hands = default!;

    [SubscribeLocalEvent]
    private void OnRelayMovement(Entity<CanEscapeInventoryComponent> ent, ref MoveInputEvent args)
    {
        if (!args.HasDirectionalMovement || ent.Comp.IsEscaping)
            return;

        if (!_container.TryGetContainingContainer((ent.Owner, null, null), out var container)
            || !_actionBlocker.CanInteract(ent.Owner, container.Owner))
            return;

        // Make sure there's nothing stopped the removal (like being glued).
        if (!_container.CanRemove(ent.Owner, container))
        {
            _popup.PopupEntity(Loc.GetString("escape-inventory-component-failed-resisting"), ent.Owner, ent.Owner);
            return;
        }

        if (IsEscapeContainer(ent.Owner, container.Owner))
            AttemptEscape(ent, container.Owner);
    }

    [SubscribeLocalEvent]
    private void OnEscape(Entity<CanEscapeInventoryComponent> ent, ref EscapeInventoryEvent args)
    {
        if (ent.Comp.DoAfterIndex is { } doAfterIndex && doAfterIndex != args.DoAfter.Index)
            return;

        if (ent.Comp.DoAfterIndex != null)
        {
            ent.Comp.DoAfterIndex = null;
            DirtyField(ent, ent.Comp, nameof(CanEscapeInventoryComponent.DoAfterIndex));
        }

        if (args.Handled || args.Cancelled)
            return;

        args.Handled = _container.TryRemoveFromContainer(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnDoAfterAttempt(Entity<CanEscapeInventoryComponent> ent, ref DoAfterAttemptEvent<EscapeInventoryEvent> args)
    {
        if (!_container.TryGetContainingContainer((ent.Owner, null, null), out var container)
            || !IsEscapeContainer(ent.Owner, container.Owner)
            || !_container.CanRemove(ent.Owner, container))
        {
            args.Cancel();
        }
    }

    private void AttemptEscape(Entity<CanEscapeInventoryComponent> ent, EntityUid container)
    {
        var doAfterEventArgs = new DoAfterArgs(EntityManager, ent.Owner, ent.Comp.BaseResistTime, new EscapeInventoryEvent(), ent.Owner, target: container)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            AttemptFrequency = AttemptFrequency.EveryTick,
        };

        if (!_doAfter.TryStartDoAfter(doAfterEventArgs, out var doAfterId)
            || !_doAfter.IsRunning(doAfterId))
            return;

        ent.Comp.DoAfterIndex = doAfterId.Value.Index;
        DirtyField(ent, ent.Comp, nameof(CanEscapeInventoryComponent.DoAfterIndex));
        _popup.PopupEntity(Loc.GetString("escape-inventory-component-start-resisting"), ent.Owner, ent.Owner);
        _popup.PopupEntity(Loc.GetString("escape-inventory-component-start-resisting-target"), container, container);
    }

    private bool IsEscapeContainer(EntityUid entity, EntityUid container)
    {
        return _hands.IsHolding(container, entity, out _)
               || HasComp<StorageComponent>(container)
               || HasComp<InventoryComponent>(container)
               || HasComp<SecretStashComponent>(container);
    }
}
