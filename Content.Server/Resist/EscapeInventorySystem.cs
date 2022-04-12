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
    }

    private void OnRelayMovement(EntityUid uid, CanEscapeInventoryComponent component, RelayMovementEntityEvent args)
    {
        EntitySystem.Get<ActionBlockerSystem>().CanMove(uid);
        if (_containerSystem.IsEntityOrParentInContainer(uid))
        {
            Transform(uid).AttachParentToContainerOrGrid(EntityManager);
        }
    }

}
