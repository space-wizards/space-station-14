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

        SubscribeLocalEvent<CanResistInventoryComponent, RelayMoveInputEvent>(OnRelayMovement);
        SubscribeLocalEvent<CanResistInventoryComponent, UpdateCanMoveEvent>(OnMoveAttempt);
    }

    private void OnRelayMovement(EntityUid uid, CanResistInventoryComponent component, RelayMoveInputEvent args)
    {
        if (_containerSystem.IsEntityOrParentInContainer(uid))
        {
            Transform(uid).AttachParentToContainerOrGrid(EntityManager);
        }
    }

    private void OnMoveAttempt(EntityUid uid, CanResistInventoryComponent component, UpdateCanMoveEvent args)
    {
        if (_containerSystem.IsEntityOrParentInContainer(uid))
            args.Cancel();
    }

    private void AttemptEscape(EntityUid user, EntityUid container, CanResistInventoryComponent component)
    {
    }
}
