using Content.Shared.Popups;
using Content.Shared.Storage.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Movement.Events;
using Content.Shared.Resist.Components;
using Content.Shared.Storage;
using Robust.Shared.Containers;

namespace Content.Shared.Resist.EntitySystems;

/// <summary>
/// Handles allowing entities with <see cref="CanEscapeInventoryComponent"/> to escape from containers or inventories by moving.
/// </summary>
public sealed class EscapeInventorySystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CanEscapeInventoryComponent, MoveInputEvent>(OnRelayMovement);
        SubscribeLocalEvent<CanEscapeInventoryComponent, EscapeInventoryEvent>(OnEscape);
        SubscribeLocalEvent<CanEscapeInventoryComponent, DroppedEvent>(OnDropped);
    }

    private void OnRelayMovement(Entity<CanEscapeInventoryComponent> ent, ref MoveInputEvent args)
    {
        if (!args.HasDirectionalMovement)
            return;

        if (!_container.TryGetContainingContainer((ent.Owner, null, null), out var container)
            || !_actionBlocker.CanInteract(ent.Owner, container.Owner))
            return;

        // Make sure there's nothing stopped the removal (like being glued).
        if (!_container.CanRemove(ent.Owner, container))
        {
            _popup.PopupClient(Loc.GetString("escape-inventory-component-failed-resisting"), ent.Owner, ent.Owner);
            return;
        }

        if (_hands.IsHolding(container.Owner, ent.Owner, out _)
            || HasComp<StorageComponent>(container.Owner)
            || HasComp<InventoryComponent>(container.Owner)
            || HasComp<SecretStashComponent>(container.Owner))
        {
            AttemptEscape(ent, container.Owner);
        }
    }

    private void AttemptEscape(Entity<CanEscapeInventoryComponent> ent, EntityUid container, float multiplier = 1f)
    {
        if (ent.Comp.IsEscaping)
            return;

        var doAfterEventArgs = new DoAfterArgs(EntityManager, ent.Owner, ent.Comp.BaseResistTime * multiplier, new EscapeInventoryEvent(), ent.Owner, target: container)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = false
        };

        if (!_doAfter.TryStartDoAfter(doAfterEventArgs, out ent.Comp.DoAfter))
            return;

        _popup.PopupClient(Loc.GetString("escape-inventory-component-start-resisting"), ent.Owner, ent.Owner);
        _popup.PopupEntity(Loc.GetString("escape-inventory-component-start-resisting-target"), container, container);

        Dirty(ent);
    }

    private void OnEscape(Entity<CanEscapeInventoryComponent> ent, ref EscapeInventoryEvent args)
    {
        ent.Comp.DoAfter = null;

        if (args.Handled || args.Cancelled)
            return;

        _container.AttachParentToContainerOrGrid((ent.Owner, Transform(ent.Owner)));
        args.Handled = true;
    }

    private void OnDropped(Entity<CanEscapeInventoryComponent> ent, ref DroppedEvent args)
    {
        if (ent.Comp.DoAfter == null)
            return;

        _doAfter.Cancel(ent.Comp.DoAfter);
        ent.Comp.DoAfter = null;
        Dirty(ent);
    }
}
