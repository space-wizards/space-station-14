using Content.Shared.Movement;
using Robust.Shared.GameObjects;
using Content.Server.Storage.Components;
using Content.Server.DoAfter;
using Content.Server.Lock;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using Robust.Shared.Containers;
using Content.Server.Popups;
using Robust.Shared.Localization;
using Content.Shared.ActionBlocker;
using Content.Server.Disease.Components;

namespace Content.Server.Resist;

public sealed class EscapeInventorySystem : EntitySystem
{
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CanEscapeInventoryComponent, RelayMovementEntityEvent>(OnRelayMovement);
        SubscribeLocalEvent<CanEscapeInventoryComponent, UpdateCanMoveEvent>(OnMoveAttempt);
    }

    private void OnRelayMovement(EntityUid uid, CanEscapeInventoryComponent component, RelayMovementEntityEvent args)
    {
        if (_containerSystem.IsEntityOrParentInContainer(args.Entity))
        {
            Transform(args.Entity).AttachParentToContainerOrGrid(EntityManager);
        }
    }

    private void OnMoveAttempt(EntityUid uid, CanEscapeInventoryComponent component, UpdateCanMoveEvent args)
    {
        if (_containerSystem.IsEntityOrParentInContainer(uid))
            args.Cancel();
    }

    private void AttemptEscape(EntityUid user, EntityUid container, CanEscapeInventoryComponent component)
    {
    }
}
