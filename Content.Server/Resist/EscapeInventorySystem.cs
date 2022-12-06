using Content.Server.DoAfter;
using Content.Server.Contests;
using Robust.Shared.Containers;
using Content.Server.Popups;
using Robust.Shared.Player;
using Content.Shared.Storage;
using Content.Shared.Inventory;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.ActionBlocker;
using Content.Shared.Movement.Events;
using Content.Shared.Interaction.Events;

namespace Content.Server.Resist;

public sealed class EscapeInventorySystem : EntitySystem
{
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly ContestsSystem _contests = default!;

    /// <summary>
    /// You can't escape the hands of an entity this many times more massive than you.
    /// </summary>
    public const float MaximumMassDisadvantage = 6f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CanEscapeInventoryComponent, MoveInputEvent>(OnRelayMovement);
        SubscribeLocalEvent<CanEscapeInventoryComponent, EscapeDoAfterComplete>(OnEscapeComplete);
        SubscribeLocalEvent<CanEscapeInventoryComponent, EscapeDoAfterCancel>(OnEscapeFail);
        SubscribeLocalEvent<CanEscapeInventoryComponent, DroppedEvent>(OnDropped);
    }

    private void OnRelayMovement(EntityUid uid, CanEscapeInventoryComponent component, ref MoveInputEvent args)
    {
        if (component.CancelToken != null)
            return;

        if (!_containerSystem.TryGetContainingContainer(uid, out var container) || !_actionBlockerSystem.CanInteract(uid, container.Owner))
            return;

        // Contested
        if (_handsSystem.IsHolding(container.Owner, uid, out var inHand))
        {
            var contestResults = _contests.MassContest(uid, container.Owner);

            // Inverse if we aren't going to divide by 0, otherwise just use a default multiplier of 1.
            if (contestResults != 0)
                contestResults = 1 / contestResults;
            else
                contestResults = 1;

            if (contestResults >= MaximumMassDisadvantage)
                return;

            AttemptEscape(uid, container.Owner, component, contestResults);
            return;
        }

        // Uncontested
        if (HasComp<SharedStorageComponent>(container.Owner) || HasComp<InventoryComponent>(container.Owner))
            AttemptEscape(uid, container.Owner, component);
    }

    private void AttemptEscape(EntityUid user, EntityUid container, CanEscapeInventoryComponent component, float multiplier = 1f)
    {
        component.CancelToken = new();
        var doAfterEventArgs = new DoAfterEventArgs(user, component.BaseResistTime * multiplier, component.CancelToken.Token, container)
        {
            BreakOnTargetMove = false,
            BreakOnUserMove = false,
            BreakOnDamage = true,
            BreakOnStun = true,
            NeedHand = false,
            UserFinishedEvent = new EscapeDoAfterComplete(),
            UserCancelledEvent = new EscapeDoAfterCancel(),
        };

        _popupSystem.PopupEntity(Loc.GetString("escape-inventory-component-start-resisting"), user, Filter.Entities(user));
        _popupSystem.PopupEntity(Loc.GetString("escape-inventory-component-start-resisting-target"), container, Filter.Entities(container));
        _doAfterSystem.DoAfter(doAfterEventArgs);
    }

    private void OnEscapeComplete(EntityUid uid, CanEscapeInventoryComponent component, EscapeDoAfterComplete ev)
    {
        //Drops the mob on the tile below the container
        Transform(uid).AttachParentToContainerOrGrid(EntityManager);
        component.CancelToken = null;
    }

    private void OnEscapeFail(EntityUid uid, CanEscapeInventoryComponent component, EscapeDoAfterCancel ev)
    {
        component.CancelToken = null;
    }

    private void OnDropped(EntityUid uid, CanEscapeInventoryComponent component, DroppedEvent args)
    {
        component.CancelToken?.Cancel();
    }

    private sealed class EscapeDoAfterComplete : EntityEventArgs { }

    private sealed class EscapeDoAfterCancel : EntityEventArgs { }
}
