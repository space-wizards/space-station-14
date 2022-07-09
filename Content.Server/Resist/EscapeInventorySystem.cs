using Content.Shared.Movement;
using Content.Server.DoAfter;
using Robust.Shared.Containers;
using Content.Server.Popups;
using Robust.Shared.Player;
using Content.Shared.Storage;
using Content.Shared.Inventory;
using Content.Shared.Hands.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Movement.Events;

namespace Content.Server.Resist;

public sealed class EscapeInventorySystem : EntitySystem
{
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CanEscapeInventoryComponent, RelayMoveInputEvent>(OnRelayMovement);
        SubscribeLocalEvent<CanEscapeInventoryComponent, UpdateCanMoveEvent>(OnMoveAttempt);
        SubscribeLocalEvent<CanEscapeInventoryComponent, EscapeDoAfterComplete>(OnEscapeComplete);
        SubscribeLocalEvent<CanEscapeInventoryComponent, EscapeDoAfterCancel>(OnEscapeFail);
    }

    private void OnRelayMovement(EntityUid uid, CanEscapeInventoryComponent component, RelayMoveInputEvent args)
    {
        //Prevents the user from creating multiple DoAfters if they're already resisting.
        if (component.IsResisting == true)
            return;

        if (_containerSystem.TryGetContainingContainer(uid, out var container)
            && (HasComp<SharedStorageComponent>(container.Owner) || HasComp<InventoryComponent>(container.Owner) || HasComp<SharedHandsComponent>(container.Owner)))
        {
            if (_actionBlockerSystem.CanInteract(uid, container.Owner))
                AttemptEscape(uid, container.Owner, component);
        }
    }

    private void OnMoveAttempt(EntityUid uid, CanEscapeInventoryComponent component, UpdateCanMoveEvent args)
    {
        if (_containerSystem.IsEntityOrParentInContainer(uid))
            args.Cancel();
    }

    private void AttemptEscape(EntityUid user, EntityUid container, CanEscapeInventoryComponent component)
    {
        component.CancelToken = new();
        var doAfterEventArgs = new DoAfterEventArgs(user, component.ResistTime, component.CancelToken.Token, container)
        {
            BreakOnTargetMove = false,
            BreakOnUserMove = false,
            BreakOnDamage = true,
            BreakOnStun = true,
            NeedHand = false,
            UserFinishedEvent = new EscapeDoAfterComplete(),
            UserCancelledEvent = new EscapeDoAfterCancel(),
        };

        component.IsResisting = true;
        _popupSystem.PopupEntity(Loc.GetString("escape-inventory-component-start-resisting"), user, Filter.Entities(user));
        _popupSystem.PopupEntity(Loc.GetString("escape-inventory-component-start-resisting-target"), container, Filter.Entities(container));
        _doAfterSystem.DoAfter(doAfterEventArgs);
    }

    private void OnEscapeComplete(EntityUid uid, CanEscapeInventoryComponent component, EscapeDoAfterComplete ev)
    {
        //Drops the mob on the tile below the container
        Transform(uid).AttachParentToContainerOrGrid(EntityManager);
        component.IsResisting = false;
    }

    private void OnEscapeFail(EntityUid uid, CanEscapeInventoryComponent component, EscapeDoAfterCancel ev)
    {
        component.IsResisting = false;
    }

    private sealed class EscapeDoAfterComplete : EntityEventArgs { }

    private sealed class EscapeDoAfterCancel : EntityEventArgs { }
}
