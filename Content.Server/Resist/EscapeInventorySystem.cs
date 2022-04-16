using Content.Shared.Movement;
using Content.Server.DoAfter;
using Robust.Shared.Containers;
using Content.Server.Popups;
using Content.Shared.Movement.EntitySystems;

namespace Content.Server.Resist;

public sealed class EscapeInventorySystem : EntitySystem
{
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CanEscapeInventoryComponent, RelayMoveInputEvent>(OnRelayMovement);
        SubscribeLocalEvent<CanEscapeInventoryComponent, UpdateCanMoveEvent>(OnMoveAttempt);
        SubscribeLocalEvent<CanEscapeInventoryComponent, EscapeDoAfterComplete>(OnEscape);
    }

    private void OnRelayMovement(EntityUid uid, CanEscapeInventoryComponent component, RelayMoveInputEvent args)
    {
        if (_containerSystem.IsEntityOrParentInContainer(uid))
        {
            if (_containerSystem.TryGetContainingContainer(uid, out var container))
            {
                AttemptEscape(uid, container.Owner, component);
            }
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
            UserFinishedEvent = new EscapeDoAfterComplete(user),
        };

        _doAfterSystem.DoAfter(doAfterEventArgs);
    }

    private void OnEscape(EntityUid user, CanEscapeInventoryComponent component, EscapeDoAfterComplete ev)
    {
        //Drops the mob on the tile below the container
        Transform(user).AttachParentToContainerOrGrid(EntityManager);
    }

    private sealed class EscapeDoAfterComplete : EntityEventArgs
    {
        public readonly EntityUid User;

        public EscapeDoAfterComplete(EntityUid userUid)
        {
            User = userUid;
        }
    }

    private sealed class EscapeDoAfterCancelled : EntityEventArgs
    {

    }
}
